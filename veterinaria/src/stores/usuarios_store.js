import { reactive } from 'vue';
import { usuarios_service } from '@/services/usuarios_service';

export const usuarios_store = reactive({
  usuarios: [],
  roles: [],
  paginacion: { pagina: 1, tamano: 10, total: 0, hay_mas: false },
  busqueda: '',
  cargando: false,
  error: null,

  async cargar_usuarios(parametros = {}) {
    this.cargando = true;
    this.error = null;
    try {
      const params = {
        pagina: this.paginacion.pagina,
        tamano: this.paginacion.tamano,
        ...(this.busqueda ? { busqueda: this.busqueda } : {}),
        ...parametros,
      };

      const respuesta = await usuarios_service.listar_usuarios(params);
      const items = respuesta.usuarios ?? respuesta.items ?? [];
      const pag = respuesta.pagina ?? respuesta;

      if (this.paginacion.pagina === 1) {
        this.usuarios = items;
      } else {
        this.usuarios = [...this.usuarios, ...items];
      }

      this.paginacion = {
        pagina: pag.pagina ?? 1,
        tamano: pag.tamano ?? 10,
        total: pag.total ?? 0,
        hay_mas: pag.hay_mas ?? false,
      };
    } catch (e) {
      this.error = e.mensaje || e.message;
    } finally {
      this.cargando = false;
    }
  },

  async cargar_mas_usuarios() {
    if (this.paginacion.hay_mas && !this.cargando) {
      this.paginacion.pagina++;
      await this.cargar_usuarios();
    }
  },

  async buscar(texto) {
    this.busqueda = texto || '';
    this.reiniciar_paginacion();
    await this.cargar_usuarios();
  },

  reiniciar_paginacion() {
    this.paginacion.pagina = 1;
    this.usuarios = [];
  },

  async cargar_roles() {
    try {
      this.roles = await usuarios_service.listar_roles();
    } catch (e) {
      console.warn('Error al cargar roles:', e);
    }
  },

  async crear_usuario(datos) {
    const res = await usuarios_service.crear_usuario(datos);
    this.reiniciar_paginacion();
    await this.cargar_usuarios();
    return res;
  },

  async actualizar_rol(id, rolId) {
    const res = await usuarios_service.actualizar_rol(id, rolId);
    const idx = this.usuarios.findIndex((u) => u.id === id);
    if (idx !== -1) {
      this.usuarios[idx].rol_id = res.rol_id;
      this.usuarios[idx].rol_codigo = res.rol_codigo;
      this.usuarios[idx].rol_nombre = res.rol_nombre;
    }
    return res;
  },

  async eliminar_usuario(id) {
    await usuarios_service.eliminar_usuario(id);
    this.usuarios = this.usuarios.filter((u) => u.id !== id);
    if (this.paginacion.total > 0) this.paginacion.total--;
  },
});

export default usuarios_store;
