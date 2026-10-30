<template>
  <comp-page
    titulo="Trazabilidad y Auditoría"
    :mostrar_actualizar="true"
    @actualizar="refrescar"
  >
    <template #acciones>
      <ion-button
        fill="clear"
        size="small"
        :color="historial_store.solo_mios ? 'primary' : 'medium'"
        @click="alternarSoloMios"
        title="Filtrar solo mis registros"
      >
        <ion-icon slot="icon-only" :icon="personCircleOutline"></ion-icon>
      </ion-button>
    </template>

    <!-- Buscador Integrado -->
    <comp-buscador
      placeholder="Buscar por PED-XXXX, usuario, notas..."
      @buscar="onBuscar"
    />

    <!-- Filtros de Tipo (Segmentos) -->
    <div class="seccion-segmentos ion-padding-horizontal">
      <ion-segment
        :value="historial_store.tipo_filtro"
        @ionChange="cambiarTipo($event.detail.value)"
      >
        <ion-segment-button value="todos">
          <ion-label>Todos ({{ historial_store.resumen.todos }})</ion-label>
        </ion-segment-button>
        <ion-segment-button value="pedido">
          <ion-label>Pedidos ({{ historial_store.resumen.pedidos }})</ion-label>
        </ion-segment-button>
        <ion-segment-button value="entrega">
          <ion-label>Entregas ({{ historial_store.resumen.entregas }})</ion-label>
        </ion-segment-button>
      </ion-segment>

      <!-- Chip informativo si está activo 'solo_mios' -->
      <div v-if="historial_store.solo_mios" class="ion-padding-top">
        <ion-chip color="primary" @click="alternarSoloMios">
          <ion-icon :icon="personOutline"></ion-icon>
          <ion-label>Mostrando solo mis registros de auditoría</ion-label>
          <ion-icon :icon="closeCircleOutline"></ion-icon>
        </ion-chip>
      </div>
    </div>

    <!-- Indicador de carga inicial -->
    <comp-esqueleto v-if="historial_store.cargando && historial_store.historial.length === 0" />

    <!-- Estado de Error -->
    <comp-estado-error
      v-else-if="historial_store.error"
      :mensaje="historial_store.error"
      @reintentar="refrescar"
    />

    <!-- Estado Vacío -->
    <div
      v-else-if="historial_store.historial.length === 0"
      class="ion-padding ion-text-center contenedor-vacio"
    >
      <ion-icon :icon="timeOutline" class="icono-vacio"></ion-icon>
      <h3>Sin registros de auditoría</h3>
      <p>No se encontraron movimientos para los filtros seleccionados.</p>
    </div>

    <!-- Listado Paginado de Auditoría -->
    <comp-lista
      v-else
      :total="historial_store.paginacion.total"
      :cantidad_mostrada="historial_store.historial.length"
      :hay_mas="historial_store.paginacion.hay_mas"
      :cargando="historial_store.cargando"
      @ver_mas="cargarMas"
    >
      <template #lista>
        <div class="lista-auditoria ion-padding">
          <ion-card
            v-for="evento in historial_store.historial"
            :key="evento.id"
            class="tarjeta-auditoria vet-card"
          >
            <ion-card-header>
              <div class="cabecera-tarjeta">
                <div class="bloque-codigo">
                  <ion-icon
                    :icon="evento.tipo === 'pedido' ? receiptOutline : bicycleOutline"
                    :color="evento.tipo === 'pedido' ? 'primary' : 'tertiary'"
                  ></ion-icon>
                  <strong class="codigo-referencia">{{ evento.codigo_pedido || `Ref #${evento.referencia_id}` }}</strong>
                  <ion-badge
                    :color="evento.tipo === 'pedido' ? 'primary' : 'tertiary'"
                    class="badge-tipo"
                  >
                    {{ evento.tipo }}
                  </ion-badge>
                </div>

                <small class="fecha-auditoria">
                  {{ formatearFecha(evento.creado_en) }}
                </small>
              </div>
            </ion-card-header>

            <ion-card-content>
              <!-- Transición de Estado -->
              <div class="fila-transicion ion-margin-bottom">
                <ion-badge v-if="evento.estado_anterior" color="medium" class="badge-estado">
                  {{ formatearEstado(evento.estado_anterior) }}
                </ion-badge>
                <ion-badge v-else color="light" class="badge-estado">
                  Inicial
                </ion-badge>

                <ion-icon :icon="arrowForwardOutline" class="icono-flecha"></ion-icon>

                <ion-badge :color="obtenerColorBadge(evento.estado_nuevo)" class="badge-estado">
                  {{ formatearEstado(evento.estado_nuevo) }}
                </ion-badge>
              </div>

              <!-- Usuario responsable -->
              <div class="fila-dato">
                <ion-icon :icon="personOutline" class="icono-dato" color="primary"></ion-icon>
                <span class="valor-dato">
                  Operador: <strong>{{ evento.usuario_nombre || 'Sistema' }}</strong>
                </span>
              </div>

              <!-- Observación de auditoría -->
              <div v-if="evento.observacion" class="fila-dato ion-margin-top fila-obs">
                <ion-icon :icon="informationCircleOutline" class="icono-dato" color="medium"></ion-icon>
                <span class="texto-obs">{{ evento.observacion }}</span>
              </div>
            </ion-card-content>
          </ion-card>
        </div>
      </template>
    </comp-lista>
  </comp-page>
</template>

<script setup>
import { onMounted } from 'vue';
import {
  IonCard,
  IonCardHeader,
  IonCardContent,
  IonSegment,
  IonSegmentButton,
  IonLabel,
  IonBadge,
  IonButton,
  IonIcon,
  IonChip,
} from '@ionic/vue';
import {
  receiptOutline,
  bicycleOutline,
  personOutline,
  personCircleOutline,
  timeOutline,
  arrowForwardOutline,
  informationCircleOutline,
  closeCircleOutline,
} from 'ionicons/icons';
import CompPage from '@/components/estructura/comp_page.vue';
import CompBuscador from '@/components/base/comp_buscador.vue';
import CompEsqueleto from '@/components/base/comp_esqueleto.vue';
import CompEstadoError from '@/components/base/comp_estado_error.vue';
import CompLista from '@/components/base/comp_lista.vue';
import { historial_store } from '@/stores/historial_store';
import { vibrar_toque } from '@/services/vibracion_service';

async function refrescar(event = null) {
  await historial_store.cargar(true);
  if (event?.target?.complete) {
    event.target.complete();
  }
}

async function cargarMas() {
  await historial_store.cargar_mas();
}

async function cambiarTipo(tipo) {
  await vibrar_toque();
  await historial_store.filtrar_por_tipo(tipo);
}

async function onBuscar(termino) {
  await historial_store.buscar(termino);
}

async function alternarSoloMios() {
  await vibrar_toque();
  await historial_store.alternar_solo_mios();
}

function formatearFecha(fechaStr) {
  if (!fechaStr) return '';
  const d = new Date(fechaStr);
  return d.toLocaleDateString('es-AR', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatearEstado(estado) {
  if (!estado) return 'Inicial';
  const map = {
    borrador: 'Borrador',
    confirmado: 'Confirmado',
    en_preparacion: 'En preparación',
    enpreparacion: 'En preparación',
    listo: 'Listo',
    entregado: 'Entregado',
    entregada: 'Entregada',
    asignada: 'Asignada',
    en_camino: 'En camino',
    encamino: 'En camino',
    cerrado: 'Cerrado',
    cancelado: 'Cancelado',
    cancelada: 'Cancelada',
  };
  return map[estado.toLowerCase()] || estado;
}

function obtenerColorBadge(estado) {
  if (!estado) return 'medium';
  const e = estado.toLowerCase();
  if (e.includes('listo') || e.includes('entregad')) return 'success';
  if (e.includes('cancel')) return 'danger';
  if (e.includes('preparacion') || e.includes('camino')) return 'secondary';
  if (e.includes('confirm') || e.includes('asign')) return 'tertiary';
  return 'medium';
}

onMounted(() => {
  historial_store.cargar();
});
</script>

<style scoped>
.seccion-segmentos {
  margin-top: 8px;
}

.tarjeta-auditoria {
  margin-bottom: 12px;
  border-left: 4px solid var(--ion-color-step-300, #94a3b8);
}

.cabecera-tarjeta {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.bloque-codigo {
  display: flex;
  align-items: center;
  gap: 8px;
}

.codigo-referencia {
  font-size: 1.05rem;
}

.badge-tipo {
  font-size: 0.75rem;
  text-transform: uppercase;
}

.fecha-auditoria {
  color: var(--ion-color-step-500, #64748b);
  font-weight: 500;
}

.fila-transicion {
  display: flex;
  align-items: center;
  gap: 8px;
  background: var(--ion-color-step-50, #f8fafc);
  padding: 8px 12px;
  border-radius: 8px;
}

.icono-flecha {
  color: var(--ion-color-step-400, #94a3b8);
  font-size: 1rem;
}

.fila-dato {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.9rem;
}

.icono-dato {
  font-size: 1.1rem;
}

.fila-obs {
  align-items: flex-start;
}

.texto-obs {
  font-size: 0.85rem;
  color: var(--ion-color-step-600, #475569);
  font-style: italic;
}

.contenedor-vacio {
  margin-top: 40px;
}

.icono-vacio {
  font-size: 4rem;
  color: var(--ion-color-step-400, #94a3b8);
  margin-bottom: 12px;
}
</style>
