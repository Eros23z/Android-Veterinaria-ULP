import { reactive } from 'vue';
import { productos_service } from '@/services/productos_service';

export const productos_store = reactive({
  cargando: false,
  error: null,
  productos: [],
  categorias: [],
  contadores: {
    total: 0,
    disponibles: 0,
    no_disponibles: 0
  },

  /**
   * Carga el resumen de productos y categorías desde la API.
   * Maneja los estados de cargando, error y asignación de datos.
   */
  async cargar(forzar = false) {
    if (this.cargando && !forzar) return;

    this.cargando = true;
    this.error = null;

    try {
      const respuesta = await productos_service.obtener_resumen();
      this.productos = respuesta.productos || [];
      this.categorias = respuesta.categorias || [];
      this.contadores = respuesta.resumen || {
        total: this.productos.length,
        disponibles: this.productos.filter(p => p.disponible).length,
        no_disponibles: this.productos.filter(p => !p.disponible).length
      };
    } catch (err) {
      this.error = err.message || 'No fue posible cargar el catálogo de productos.';
      this.productos = [];
      this.categorias = [];
      this.contadores = {
        total: 0,
        disponibles: 0,
        no_disponibles: 0
      };
    } finally {
      this.cargando = false;
    }
  }
});

export default productos_store;
