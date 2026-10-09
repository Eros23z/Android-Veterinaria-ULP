<template>
  <comp-page titulo="Repartos y Entregas" :mostrar_actualizar="true" @actualizar="refrescar">
    <template #acciones>
      <ion-button
        fill="clear"
        size="small"
        :color="entregas_store.solo_mias ? 'primary' : 'medium'"
        @click="alternarSoloMias"
        title="Filtrar solo mis entregas"
      >
        <ion-icon slot="icon-only" :icon="personCircleOutline"></ion-icon>
      </ion-button>
    </template>

    <!-- Barra de Segmentos por Estado -->
    <div class="seccion-filtros ion-padding-horizontal ion-padding-top">
      <ion-segment
        :value="entregas_store.estado_filtro"
        scrollable
        @ionChange="cambiarFiltroSegmento($event.detail.value)"
      >
        <ion-segment-button value="todas">
          <ion-label>Todas ({{ entregas_store.resumen.total }})</ion-label>
        </ion-segment-button>
        <ion-segment-button value="pendiente">
          <ion-label>Pendientes ({{ entregas_store.resumen.pendientes }})</ion-label>
        </ion-segment-button>
        <ion-segment-button value="asignada">
          <ion-label>Asignadas ({{ entregas_store.resumen.asignadas }})</ion-label>
        </ion-segment-button>
        <ion-segment-button value="en_camino">
          <ion-label>En Camino ({{ entregas_store.resumen.en_camino }})</ion-label>
        </ion-segment-button>
        <ion-segment-button value="entregada">
          <ion-label>Entregadas ({{ entregas_store.resumen.entregadas }})</ion-label>
        </ion-segment-button>
      </ion-segment>

      <!-- Chip informativo del filtro solo_mias -->
      <div v-if="entregas_store.solo_mias" class="ion-padding-top ion-padding-bottom">
        <ion-chip color="primary" @click="alternarSoloMias">
          <ion-icon :icon="personOutline"></ion-icon>
          <ion-label>Viendo solo mis entregas asignadas</ion-label>
          <ion-icon :icon="closeCircleOutline"></ion-icon>
        </ion-chip>
      </div>
    </div>

    <!-- Indicador de carga inicial -->
    <comp-esqueleto v-if="entregas_store.cargando && entregas_store.entregas.length === 0" />

    <!-- Estado vacío -->
    <div
      v-else-if="entregas_store.entregas.length === 0"
      class="ion-padding ion-text-center contenedor-vacio"
    >
      <ion-icon :icon="bicycleOutline" class="icono-vacio"></ion-icon>
      <h3>Sin entregas registradas</h3>
      <p>No se encontraron entregas para el filtro seleccionado.</p>
    </div>

    <!-- Listado paginado de entregas -->
    <comp-lista
      v-else
      :total="entregas_store.paginacion.total"
      :cantidad_mostrada="entregas_store.entregas.length"
      :hay_mas="entregas_store.paginacion.hay_mas"
      :cargando="entregas_store.cargando"
      @ver_mas="cargarMas"
    >
      <template #lista>
        <div class="lista-tarjetas-entregas ion-padding">
          <ion-card
            v-for="entrega in entregas_store.entregas"
            :key="entrega.id"
            class="tarjeta-entrega"
          >
            <ion-card-header>
              <div class="cabecera-tarjeta">
                <div class="bloque-codigo">
                  <ion-icon :icon="receiptOutline" color="primary"></ion-icon>
                  <strong class="codigo-pedido">{{ entrega.codigo_pedido }}</strong>
                </div>
                <ion-badge :color="obtenerColorBadge(entrega.estado)" class="badge-estado">
                  {{ formatearEstado(entrega.estado) }}
                </ion-badge>
              </div>
            </ion-card-header>

            <ion-card-content>
              <!-- Datos del Cliente -->
              <div class="fila-dato">
                <ion-icon :icon="personOutline" class="icono-dato"></ion-icon>
                <span class="valor-dato"><strong>{{ entrega.cliente_nombre }}</strong></span>
              </div>

              <div v-if="entrega.cliente_telefono" class="fila-dato">
                <ion-icon :icon="callOutline" class="icono-dato"></ion-icon>
                <a :href="`tel:${entrega.cliente_telefono}`" class="enlace-telefono">
                  {{ entrega.cliente_telefono }}
                </a>
              </div>

              <!-- Dirección de Entrega -->
              <div class="fila-dato">
                <ion-icon :icon="locationOutline" class="icono-dato"></ion-icon>
                <span class="valor-dato">{{ entrega.direccion_linea || entrega.cliente_direccion || 'Sin dirección registrada' }}</span>
              </div>

              <!-- Referencia o Coordenadas -->
              <div v-if="entrega.direccion_latitud && entrega.direccion_longitud" class="fila-dato">
                <ion-icon :icon="navigateOutline" class="icono-dato"></ion-icon>
                <span class="texto-coordenadas">
                  GPS: {{ formatear_coordenadas(entrega.direccion_latitud, entrega.direccion_longitud) }}
                </span>
              </div>

              <!-- Importe Total del Pedido -->
              <div class="fila-dato ion-margin-top fila-total">
                <span class="etiqueta-total">Importe del Pedido:</span>
                <span class="monto-total">${{ Number(entrega.total).toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}</span>
              </div>

              <!-- Repartidor asignado si existe -->
              <div v-if="entrega.repartidor_email" class="fila-repartidor ion-margin-top">
                <ion-icon :icon="bicycleOutline" color="medium"></ion-icon>
                <span class="texto-repartidor">Repartidor: {{ entrega.repartidor_email }}</span>
              </div>

              <!-- Acciones de la Entrega -->
              <div class="bloque-acciones-tarjeta ion-margin-top">
                <!-- Botón Ver en el Mapa -->
                <ion-button
                  size="small"
                  fill="outline"
                  shape="round"
                  color="secondary"
                  @click="abrirMapa(entrega)"
                >
                  <ion-icon slot="start" :icon="mapOutline"></ion-icon>
                  Ver en el mapa
                </ion-button>

                <!-- Botón Tomar esta entrega (si está pendiente) -->
                <ion-button
                  v-if="entrega.estado === 'pendiente'"
                  size="small"
                  shape="round"
                  color="primary"
                  :disabled="accionEnProgresoId === entrega.id"
                  @click="tomarEntrega(entrega)"
                >
                  <ion-spinner v-if="accionEnProgresoId === entrega.id" name="crescent" slot="start"></ion-spinner>
                  <ion-icon v-else slot="start" :icon="handRightOutline"></ion-icon>
                  Tomar esta entrega
                </ion-button>

                <!-- Selector / Botones de cambio de estado -->
                <ion-button
                  v-if="entrega.estado !== 'entregada' && entrega.estado !== 'cancelada'"
                  size="small"
                  fill="solid"
                  shape="round"
                  color="dark"
                  :disabled="accionEnProgresoId === entrega.id"
                  @click="abrirOpcionesEstado(entrega)"
                >
                  <ion-icon slot="start" :icon="swapHorizontalOutline"></ion-icon>
                  Cambiar estado
                </ion-button>
              </div>
            </ion-card-content>
          </ion-card>
        </div>
      </template>
    </comp-lista>
  </comp-page>
</template>

<script setup>
import { ref, onMounted } from 'vue';
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
  IonSpinner,
  actionSheetController,
  alertController,
} from '@ionic/vue';
import {
  receiptOutline,
  personOutline,
  personCircleOutline,
  callOutline,
  locationOutline,
  navigateOutline,
  mapOutline,
  bicycleOutline,
  handRightOutline,
  swapHorizontalOutline,
  closeCircleOutline,
} from 'ionicons/icons';
import compPage from '@/components/estructura/comp_page.vue';
import compEsqueleto from '@/components/base/comp_esqueleto.vue';
import compLista from '@/components/base/comp_lista.vue';
import { entregas_store } from '@/stores/entregas_store';
import { geolocalizacion_service, formatear_coordenadas } from '@/services/geolocalizacion_service';
import { vibrar_toque, vibrar_error } from '@/services/vibracion_service';

const accionEnProgresoId = ref(null);

async function refrescar(event = null) {
  await entregas_store.cargar(true);
  if (event?.target?.complete) {
    event.target.complete();
  }
}

async function cargarMas() {
  await entregas_store.cargar_mas();
}

async function cambiarFiltroSegmento(valor) {
  await vibrar_toque();
  await entregas_store.filtrar_por_estado(valor);
}

async function alternarSoloMias() {
  await vibrar_toque();
  await entregas_store.alternar_solo_mias();
}

function abrirMapa(entrega) {
  vibrar_toque();
  const url = geolocalizacion_service.url_mapa(
    entrega.direccion_latitud,
    entrega.direccion_longitud,
    entrega.direccion_linea || entrega.cliente_direccion
  );
  window.open(url, '_blank', 'noopener,noreferrer');
}

async function tomarEntrega(entrega) {
  accionEnProgresoId.value = entrega.id;
  try {
    await vibrar_toque();
    await entregas_store.asignar(entrega.id, null);
    const alerta = await alertController.create({
      header: 'Entrega Asignada',
      message: `Has tomado la entrega del pedido ${entrega.codigo_pedido}. Pasa a estado "Asignada".`,
      buttons: ['Aceptar'],
    });
    await alerta.present();
  } catch (error) {
    await vibrar_error();
    const alerta = await alertController.create({
      header: 'Error al tomar entrega',
      message: error.mensaje || error.message || 'No se pudo asignar la entrega.',
      buttons: ['Cerrar'],
    });
    await alerta.present();
  } finally {
    accionEnProgresoId.value = null;
  }
}

async function abrirOpcionesEstado(entrega) {
  await vibrar_toque();

  const botones = [];

  if (entrega.estado === 'pendiente') {
    botones.push({
      text: 'Tomar entrega (Asignar a mí)',
      icon: handRightOutline,
      handler: () => tomarEntrega(entrega),
    });
    botones.push({
      text: 'Cancelar entrega',
      role: 'destructive',
      handler: () => ejecutarCambioEstado(entrega, 'cancelada'),
    });
  } else if (entrega.estado === 'asignada') {
    botones.push({
      text: 'Iniciar reparto (En Camino)',
      icon: bicycleOutline,
      handler: () => ejecutarCambioEstado(entrega, 'en_camino'),
    });
    botones.push({
      text: 'Cancelar entrega',
      role: 'destructive',
      handler: () => ejecutarCambioEstado(entrega, 'cancelada'),
    });
  } else if (entrega.estado === 'en_camino') {
    botones.push({
      text: 'Marcar como Entregada',
      icon: navigateOutline,
      handler: () => ejecutarCambioEstado(entrega, 'entregada'),
    });
    botones.push({
      text: 'Cancelar entrega',
      role: 'destructive',
      handler: () => ejecutarCambioEstado(entrega, 'cancelada'),
    });
  }

  botones.push({
    text: 'Cerrar',
    role: 'cancel',
  });

  const sheet = await actionSheetController.create({
    header: `Estado de ${entrega.codigo_pedido}`,
    subHeader: `Estado actual: ${formatearEstado(entrega.estado)}`,
    buttons: botones,
  });

  await sheet.present();
}

async function ejecutarCambioEstado(entrega, nuevoEstado) {
  accionEnProgresoId.value = entrega.id;
  try {
    await vibrar_toque();
    await entregas_store.cambiar_estado(entrega.id, nuevoEstado);
  } catch (error) {
    await vibrar_error();
    const alerta = await alertController.create({
      header: 'Error al cambiar estado',
      message: error.mensaje || error.message || 'No se pudo actualizar el estado de la entrega.',
      buttons: ['Aceptar'],
    });
    await alerta.present();
  } finally {
    accionEnProgresoId.value = null;
  }
}

function formatearEstado(estado) {
  if (!estado) return '';
  switch (String(estado).toLowerCase()) {
    case 'pendiente':
      return 'Pendiente';
    case 'asignada':
      return 'Asignada';
    case 'en_camino':
      return 'En camino';
    case 'entregada':
      return 'Entregada';
    case 'cancelada':
      return 'Cancelada';
    default:
      return estado;
  }
}

function obtenerColorBadge(estado) {
  switch (String(estado).toLowerCase()) {
    case 'pendiente':
      return 'warning';
    case 'asignada':
      return 'primary';
    case 'en_camino':
      return 'secondary';
    case 'entregada':
      return 'success';
    case 'cancelada':
      return 'danger';
    default:
      return 'medium';
  }
}

onMounted(() => {
  entregas_store.cargar(true);
});
</script>

<style scoped>
.seccion-filtros {
  background: var(--ion-color-step-50, #f8fafc);
  border-bottom: 1px solid var(--ion-color-step-150, #e2e8f0);
}

.lista-tarjetas-entregas {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding-bottom: 24px;
}

.tarjeta-entrega {
  margin: 0;
  border-radius: 16px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
  border: 1px solid var(--ion-color-step-150, #e2e8f0);
  background: #ffffff;
}

.cabecera-tarjeta {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.bloque-codigo {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 1.1rem;
}

.codigo-pedido {
  color: var(--ion-color-primary, #3880ff);
  letter-spacing: 0.5px;
}

.badge-estado {
  font-size: 11px;
  padding: 4px 8px;
  border-radius: 6px;
}

.fila-dato {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  margin-bottom: 6px;
  font-size: 0.92rem;
  color: var(--ion-color-step-800, #1e293b);
}

.icono-dato {
  font-size: 18px;
  color: var(--ion-color-medium, #64748b);
  margin-top: 1px;
  flex-shrink: 0;
}

.enlace-telefono {
  color: var(--ion-color-primary, #3880ff);
  text-decoration: none;
  font-weight: 500;
}

.texto-coordenadas {
  font-family: monospace;
  font-size: 0.82rem;
  color: var(--ion-color-medium, #64748b);
}

.fila-total {
  border-top: 1px dashed var(--ion-color-step-150, #e2e8f0);
  padding-top: 8px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.etiqueta-total {
  font-weight: 600;
  color: var(--ion-color-step-700, #334155);
}

.monto-total {
  font-size: 1.15rem;
  font-weight: 700;
  color: var(--ion-color-success, #10b981);
}

.fila-repartidor {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.82rem;
  color: var(--ion-color-medium, #64748b);
  background: var(--ion-color-step-50, #f8fafc);
  padding: 6px 10px;
  border-radius: 8px;
}

.bloque-acciones-tarjeta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
  justify-content: flex-end;
}

.contenedor-vacio {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: var(--ion-color-medium, #94a3b8);
}

.icono-vacio {
  font-size: 64px;
  margin-bottom: 12px;
}
</style>
