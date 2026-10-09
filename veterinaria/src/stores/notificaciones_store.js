import { reactive } from 'vue';
import { notificaciones_service } from '../services/notificaciones_service';

export const notificaciones_store = reactive({
  notificaciones: [],
  no_leidas: 0,
  total: 0,
  cargando: false,
  error: null,
  paginacion: {
    pagina: 1,
    tamano: 15,
    total: 0,
    hay_mas: false,
  },

  async cargar(reiniciar = true) {
    if (reiniciar) {
      this.paginacion.pagina = 1;
      this.notificaciones = [];
    }

    this.cargando = true;
    this.error = null;

    try {
      const respuesta = await notificaciones_service.obtener_notificaciones({
        pagina: this.paginacion.pagina,
        tamano: this.paginacion.tamano,
      });

      const items = respuesta.notificaciones ?? [];
      const res = respuesta.resumen ?? {};
      const pag = respuesta.pagina ?? {};

      if (this.paginacion.pagina === 1) {
        this.notificaciones = items;
      } else {
        this.notificaciones = [...this.notificaciones, ...items];
      }

      this.no_leidas = res.no_leidas ?? 0;
      this.total = res.total ?? (this.notificaciones.length);

      this.paginacion = {
        pagina: pag.pagina ?? this.paginacion.pagina,
        tamano: pag.tamano ?? this.paginacion.tamano,
        total: pag.total ?? this.total,
        hay_mas: pag.hay_mas ?? false,
      };
    } catch (err) {
      this.error = err.mensaje || err.message || 'Error al cargar notificaciones.';
    } finally {
      this.cargando = false;
    }
  },

  async cargar_mas() {
    if (this.paginacion.hay_mas && !this.cargando) {
      this.paginacion.pagina++;
      await this.cargar(false);
    }
  },

  async marcar_leida(id) {
    try {
      await notificaciones_service.marcar_leida(id);
      const notif = this.notificaciones.find((n) => n.id === id);
      if (notif && !notif.es_leida) {
        notif.es_leida = true;
        notif.leida_en = new Date().toISOString();
        if (this.no_leidas > 0) {
          this.no_leidas--;
        }
      }
    } catch (err) {
      console.error('Error al marcar notificación como leída:', err);
      throw err;
    }
  },

  async marcar_todas_leidas() {
    try {
      await notificaciones_service.marcar_todas_leidas();
      const ahora = new Date().toISOString();
      for (const n of this.notificaciones) {
        n.es_leida = true;
        n.leida_en = ahora;
      }
      this.no_leidas = 0;
    } catch (err) {
      console.error('Error al marcar todas las notificaciones como leídas:', err);
      throw err;
    }
  },
});

export default notificaciones_store;
