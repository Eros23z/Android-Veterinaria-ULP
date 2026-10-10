import { reactive } from 'vue';
import { pedidos_service } from '../services/pedidos_service';
import { offline_store } from './offline_store';

/**
 * Aplica el estado pendiente si existe una operación encolada sin sincronizar para el pedido.
 * @param {object} pedido
 * @returns {object}
 */
export function con_cambios_sin_enviar(pedido) {
  if (!pedido) return pedido;
  const copia = { ...pedido };
  const pendiente = offline_store.cola.find((c) => String(c.pedido_id) === String(copia.id));

  if (pendiente) {
    copia.estado = pendiente.nuevo_estado;
    copia.sin_enviar = true;
  } else {
    copia.sin_enviar = false;
  }

  return copia;
}

export const pedidos_store = reactive({
  pedidos: [],
  pedido_actual: null,
  paginacion: { pagina: 1, tamano: 10, total: 0, hay_mas: false },
  busqueda: '',
  estado_filtro: null,
  cargando: false,
  error: null,

  async cargar_pedidos(parametros = {}) {
    this.cargando = true;
    this.error = null;
    try {
      const params = {
        pagina: this.paginacion.pagina,
        tamano: this.paginacion.tamano,
        ...(this.busqueda ? { busqueda: this.busqueda } : {}),
        ...(this.estado_filtro ? { estado: this.estado_filtro } : {}),
        ...parametros,
      };

      const respuesta = await pedidos_service.listar_pedidos(params);
      const itemsRaw = respuesta.pedidos ?? respuesta.items ?? [];
      const pag = respuesta.pagina && typeof respuesta.pagina === 'object' ? respuesta.pagina : respuesta;

      const itemsMapeados = itemsRaw.map((p) => con_cambios_sin_enviar(p));

      if (this.paginacion.pagina === 1) {
        this.pedidos = itemsMapeados;
      } else {
        this.pedidos = [...this.pedidos, ...itemsMapeados];
      }

      this.paginacion = {
        pagina: pag.pagina ?? 1,
        tamano: pag.tamano ?? 10,
        total: pag.total ?? itemsMapeados.length,
        hay_mas: pag.hay_mas ?? false,
      };
    } catch (error) {
      this.error = error.message;
    } finally {
      this.cargando = false;
    }
  },

  async obtener_pedido(id) {
    this.cargando = true;
    this.error = null;
    try {
      const detalle = await pedidos_service.obtener_pedido(id);
      this.pedido_actual = con_cambios_sin_enviar(detalle);
      return this.pedido_actual;
    } catch (error) {
      this.error = error.message;
      throw error;
    } finally {
      this.cargando = false;
    }
  },

  async buscar(texto) {
    this.busqueda = texto || '';
    this.reiniciar_paginacion();
    await this.cargar_pedidos();
  },

  async filtrar_por_estado(estado) {
    this.estado_filtro = estado || null;
    this.reiniciar_paginacion();
    await this.cargar_pedidos();
  },

  async cargar_mas_pedidos() {
    if (this.paginacion.hay_mas && !this.cargando) {
      this.paginacion.pagina++;
      await this.cargar_pedidos();
    }
  },

  async cambiar_estado_pedido(id, nuevo_estado) {
    this.cargando = true;
    this.error = null;
    try {
      if (offline_store.sin_conexion) {
        offline_store.encolar(id, nuevo_estado);

        const pedidoIndex = this.pedidos.findIndex((p) => String(p.id) === String(id));
        if (pedidoIndex !== -1) {
          this.pedidos[pedidoIndex].estado = nuevo_estado;
          this.pedidos[pedidoIndex].sin_enviar = true;
        }

        if (this.pedido_actual && String(this.pedido_actual.id) === String(id)) {
          this.pedido_actual.estado = nuevo_estado;
          this.pedido_actual.sin_enviar = true;
        }

        return 'pendiente';
      }

      const respuesta = await pedidos_service.cambiar_estado(id, nuevo_estado);
      const pedidoIndex = this.pedidos.findIndex((p) => String(p.id) === String(id));
      if (pedidoIndex !== -1) {
        this.pedidos[pedidoIndex].estado = nuevo_estado;
        this.pedidos[pedidoIndex].sin_enviar = false;
      }

      if (this.pedido_actual && String(this.pedido_actual.id) === String(id)) {
        this.pedido_actual.estado = nuevo_estado;
        this.pedido_actual.sin_enviar = false;
      }

      return respuesta;
    } catch (error) {
      this.error = error.message;
      throw error;
    } finally {
      this.cargando = false;
    }
  },

  reiniciar_paginacion() {
    this.paginacion.pagina = 1;
    this.pedidos = [];
  },
});

export default pedidos_store;
