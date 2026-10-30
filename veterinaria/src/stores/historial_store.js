import { reactive } from 'vue';
import { historial_service } from '@/services/historial_service';

export const historial_store = reactive({
  cargando: false,
  error: null,
  historial: [],
  tipo_filtro: 'todos',
  busqueda: '',
  solo_mios: false,
  resumen: {
    todos: 0,
    pedidos: 0,
    entregas: 0,
  },
  paginacion: {
    pagina: 1,
    tamano: 15,
    total: 0,
    hay_mas: false,
  },

  async cargar(forzar = false) {
    if (this.cargando && !forzar) return;
    this.cargando = true;
    this.error = null;
    this.paginacion.pagina = 1;

    try {
      const res = await historial_service.obtener_historial({
        tipo: this.tipo_filtro !== 'todos' ? this.tipo_filtro : undefined,
        busqueda: this.busqueda || undefined,
        solo_mios: this.solo_mios || undefined,
        pagina: 1,
        tamano: this.paginacion.tamano,
      });

      this.historial = res.historial || [];
      if (res.resumen) {
        this.resumen = res.resumen;
      }
      if (res.paginacion) {
        this.paginacion.total = res.paginacion.total || 0;
        this.paginacion.hay_mas = res.paginacion.hay_mas || false;
      }
    } catch (err) {
      console.error('Error al cargar historial:', err);
      this.error = err.message || 'No fue posible cargar el historial de eventos.';
      this.historial = [];
    } finally {
      this.cargando = false;
    }
  },

  async cargar_mas() {
    if (this.cargando || !this.paginacion.hay_mas) return;
    this.cargando = true;
    const siguientePagina = this.paginacion.pagina + 1;

    try {
      const res = await historial_service.obtener_historial({
        tipo: this.tipo_filtro !== 'todos' ? this.tipo_filtro : undefined,
        busqueda: this.busqueda || undefined,
        solo_mios: this.solo_mios || undefined,
        pagina: siguientePagina,
        tamano: this.paginacion.tamano,
      });

      if (res.historial && res.historial.length > 0) {
        this.historial = [...this.historial, ...res.historial];
        this.paginacion.pagina = siguientePagina;
      }
      if (res.paginacion) {
        this.paginacion.hay_mas = res.paginacion.hay_mas || false;
      }
    } catch (err) {
      console.error('Error al cargar más historial:', err);
    } finally {
      this.cargando = false;
    }
  },

  async filtrar_por_tipo(nuevoTipo) {
    this.tipo_filtro = nuevoTipo;
    await this.cargar(true);
  },

  async buscar(texto) {
    this.busqueda = texto;
    await this.cargar(true);
  },

  async alternar_solo_mios() {
    this.solo_mios = !this.solo_mios;
    await this.cargar(true);
  },
});

export default historial_store;
