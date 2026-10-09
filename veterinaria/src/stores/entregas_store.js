import { reactive } from 'vue';
import { entregas_service } from '../services/entregas_service';

export const entregas_store = reactive({
  entregas: [],
  resumen: {
    total: 0,
    pendientes: 0,
    asignadas: 0,
    en_camino: 0,
    entregadas: 0,
    canceladas: 0,
  },
  estado_filtro: 'todas', // 'todas', 'pendiente', 'asignada', 'en_camino', 'entregada', 'cancelada'
  solo_mias: false,
  cargando: false,
  error: null,
  paginacion: {
    pagina: 1,
    tamano: 10,
    total: 0,
    hay_mas: false,
  },

  async cargar(reiniciar = true) {
    if (reiniciar) {
      this.paginacion.pagina = 1;
      this.entregas = [];
    }

    this.cargando = true;
    this.error = null;

    try {
      const params = {
        pagina: this.paginacion.pagina,
        tamano: this.paginacion.tamano,
        ...(this.estado_filtro && this.estado_filtro !== 'todas' ? { estados: this.estado_filtro } : {}),
        ...(this.solo_mias ? { solo_mias: true } : {}),
      };

      const respuesta = await entregas_service.obtener_entregas(params);
      const items = respuesta.entregas ?? [];
      const res = respuesta.resumen ?? {};
      const pag = respuesta.pagina ?? {};

      if (this.paginacion.pagina === 1) {
        this.entregas = items;
      } else {
        this.entregas = [...this.entregas, ...items];
      }

      this.resumen = {
        total: res.total ?? 0,
        pendientes: res.pendientes ?? 0,
        asignadas: res.asignadas ?? 0,
        en_camino: res.en_camino ?? 0,
        entregadas: res.entregadas ?? 0,
        canceladas: res.canceladas ?? 0,
      };

      this.paginacion = {
        pagina: pag.pagina ?? this.paginacion.pagina,
        tamano: pag.tamano ?? this.paginacion.tamano,
        total: pag.total ?? this.entregas.length,
        hay_mas: pag.hay_mas ?? false,
      };
    } catch (err) {
      this.error = err.mensaje || err.message || 'Error al cargar las entregas.';
    } finally {
      this.cargando = false;
    }
  },

  async filtrar_por_estado(nuevo_estado) {
    this.estado_filtro = nuevo_estado;
    await this.cargar(true);
  },

  async alternar_solo_mias() {
    this.solo_mias = !this.solo_mias;
    await this.cargar(true);
  },

  async cargar_mas() {
    if (this.paginacion.hay_mas && !this.cargando) {
      this.paginacion.pagina++;
      await this.cargar(false);
    }
  },

  async asignar(id, repartidor_id = null) {
    this.cargando = true;
    this.error = null;
    try {
      await entregas_service.asignar(id, repartidor_id);
      await this.cargar(true);
    } catch (err) {
      this.error = err.mensaje || err.message || 'Error al asignar la entrega.';
      throw err;
    } finally {
      this.cargando = false;
    }
  },

  async cambiar_estado(id, nuevo_estado) {
    this.cargando = true;
    this.error = null;
    try {
      await entregas_service.cambiar_estado(id, nuevo_estado);
      await this.cargar(true);
    } catch (err) {
      this.error = err.mensaje || err.message || 'Error al cambiar estado de la entrega.';
      throw err;
    } finally {
      this.cargando = false;
    }
  },
});

export default entregas_store;
