import { reactive } from 'vue';
import { token_service } from '@/services/token_service';
import { sesion_service } from '@/services/sesion_service';
import { biometria_service } from '@/services/biometria_service';
import { al_expirar_sesion } from '@/services/ajax_service';

export const sesion_store = reactive({
  usuario: null,
  autenticado: false,
  rol_codigo: null,
  rol_nombre: null,
  restaurando: true,
  biometria_disponible: false,
  hay_token_guardado: false,
  puede_usar_huella: false,

  /**
   * Al iniciar la app:
   * a) Verifica si hay biometría disponible en el teléfono.
   * b) Comprueba si existe un token/refresh_token guardado.
   * c) Expone el booleano reactivo puede_usar_huella = biometria_disponible && hay_token_guardado.
   * d) Pone restaurando = false y deja autenticado = false (NO hace auto-login).
   */
  async inicializar() {
    this.restaurando = true;
    try {
      await token_service.inicializar_tokens();

      // a) Verificar disponibilidad de biometría en el teléfono
      this.biometria_disponible = await biometria_service.verificar_disponibilidad();

      // b) Comprobar si existe un token/refresh_token guardado en token_service
      const token = token_service.obtener_token();
      const refreshToken = token_service.obtener_refresh_token();
      this.hay_token_guardado = !!(token || refreshToken);

      // c) Exponer booleano reactivo puede_usar_huella
      this.puede_usar_huella = this.biometria_disponible && this.hay_token_guardado;

      // d) Dejar autenticado = false
      this.autenticado = false;
      this.usuario = null;
      this.rol_codigo = null;
      this.rol_nombre = null;
    } catch (e) {
      console.error('Error al inicializar sesión:', e);
      this.biometria_disponible = false;
      this.hay_token_guardado = false;
      this.puede_usar_huella = false;
      this.autenticado = false;
      this.usuario = null;
    } finally {
      this.restaurando = false;
    }
  },

  async comprobar_sesion_guardada() {
    return await this.inicializar();
  },

  async restaurar_sesion() {
    return await this.inicializar();
  },

  establecer_usuario_autenticado(perfil) {
    this.usuario = perfil;
    this.autenticado = true;
    this.rol_codigo = perfil.rol_codigo || null;
    this.rol_nombre = perfil.rol_nombre || null;
    this.hay_token_guardado = true;
    this.puede_usar_huella = this.biometria_disponible && this.hay_token_guardado;
  },

  async login(email, password) {
    const respuesta = await sesion_service.login(email, password);
    await token_service.guardar_tokens(respuesta.token, respuesta.refresh_token);

    this.establecer_usuario_autenticado(respuesta.usuario);

    // Actualizar disponibilidad biométrica tras login
    this.biometria_disponible = await biometria_service.verificar_disponibilidad();
    this.puede_usar_huella = this.biometria_disponible && this.hay_token_guardado;

    return respuesta;
  },

  /**
   * Ingreso con biometría según diseño Unidad 5:
   * 1. Llama a biometria_service.autenticar("Confirma tu identidad para entrar").
   * 2. Si es rechazada o cancelada, retorna false (permanece en login sin hacer nada).
   * 3. Si es exitosa, valida sesión con GET /api/sesion/yo (o refresh si da 401).
   * 4. Si la validación es exitosa, autenticado = true y carga el usuario.
   */
  async ingresar_con_huella() {
    const autorizada = await biometria_service.autenticar('Confirma tu identidad para entrar');
    if (!autorizada) {
      return false;
    }

    const refreshToken = token_service.obtener_refresh_token();
    let perfil = null;

    try {
      try {
        perfil = await sesion_service.obtener_perfil();
      } catch (err) {
        if (err.status === 401 && refreshToken) {
          const resRefresh = await sesion_service.refresh(refreshToken);
          await token_service.guardar_tokens(resRefresh.token, resRefresh.refresh_token);
          perfil = resRefresh.usuario;
        } else {
          throw err;
        }
      }

      this.establecer_usuario_autenticado(perfil);
      return true;
    } catch (error) {
      await token_service.limpiar_tokens();
      this.hay_token_guardado = false;
      this.puede_usar_huella = false;
      this._limpiar_estado();
      const errSesion = new Error('La sesión expiró. Ingrese con sus credenciales.');
      errSesion.status = 401;
      throw errSesion;
    }
  },

  async logout() {
    const refreshToken = token_service.obtener_refresh_token();
    try {
      if (refreshToken) {
        await sesion_service.logout(refreshToken).catch(() => {});
      }
    } finally {
      await token_service.limpiar_tokens();
      this.hay_token_guardado = false;
      this.puede_usar_huella = false;
      this._limpiar_estado();

      try {
        const routerModule = await import('@/router');
        const r = routerModule.default;
        if (r && r.currentRoute?.value?.path !== '/login') {
          await r.replace('/login');
        }
      } catch (errorNavegacion) {
        console.debug('Aviso de navegación al cerrar sesión:', errorNavegacion);
      }
    }
  },

  tiene_rol(rolesPermitidos) {
    if (!rolesPermitidos || rolesPermitidos.length === 0) return true;
    if (rolesPermitidos.includes('*')) return true;
    if (!this.rol_codigo) return false;
    return rolesPermitidos.includes(this.rol_codigo);
  },

  _limpiar_estado() {
    this.usuario = null;
    this.autenticado = false;
    this.rol_codigo = null;
    this.rol_nombre = null;
  },
});

// Suscribirse al evento de expiración de sesión del servicio ajax
al_expirar_sesion(async () => {
  sesion_store.hay_token_guardado = false;
  sesion_store.puede_usar_huella = false;
  sesion_store._limpiar_estado();
  try {
    const routerModule = await import('@/router');
    const r = routerModule.default;
    if (r && r.currentRoute?.value?.path !== '/login') {
      await r.replace('/login');
    }
  } catch (errorNavegacion) {
    console.debug('Aviso de navegación tras expirar sesión:', errorNavegacion);
  }
});

export default sesion_store;
