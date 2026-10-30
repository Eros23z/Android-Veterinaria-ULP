import { reactive } from 'vue';
import { comandas_service } from '@/services/comandas_service';
import { vibrar_toque, vibrar_exito, vibrar_error } from '@/services/vibracion_service';

export const comandas_store = reactive({
  cargando: false,
  error: null,
  comandas: [],

  async cargar(forzar = false) {
    if (this.cargando && !forzar) return;
    this.cargando = true;
    this.error = null;

    try {
      const res = await comandas_service.listar();
      this.comandas = res.comandas || [];
    } catch (err) {
      console.error('Error al cargar comandas:', err);
      this.error = err.message || 'No fue posible cargar las comandas de preparación.';
      this.comandas = [];
    } finally {
      this.cargando = false;
    }
  },

  async mover_item(itemId, nuevoEstado, pedidoId) {
    await vibrar_toque();
    try {
      const res = await comandas_service.mover_item(itemId, nuevoEstado);
      // Actualizar localmente el ítem y pedido
      const comanda = this.comandas.find((c) => c.id === pedidoId);
      if (comanda) {
        const item = comanda.items.find((i) => i.id === itemId);
        if (item) {
          item.estado = res.item_estado;
        }
        comanda.estado = res.pedido_estado;
        comanda.items_listos = comanda.items.filter((i) => i.estado === 'listo').length;
      }
      await vibrar_exito();
      return res;
    } catch (err) {
      await vibrar_error();
      throw err;
    }
  },

  async marcar_lista(pedidoId) {
    await vibrar_toque();
    try {
      const res = await comandas_service.marcar_lista(pedidoId);
      const comanda = this.comandas.find((c) => c.id === pedidoId);
      if (comanda) {
        comanda.items.forEach((i) => {
          i.estado = 'listo';
        });
        comanda.estado = res.estado || 'listo';
        comanda.items_listos = comanda.items.length;
      }
      await vibrar_exito();
      return res;
    } catch (err) {
      await vibrar_error();
      throw err;
    }
  },
});

export default comandas_store;
