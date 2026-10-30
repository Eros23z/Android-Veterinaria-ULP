<template>
  <comp-page
    titulo="Preparación de Comandas"
    :mostrar_actualizar="true"
    @actualizar="refrescar"
  >
    <div class="cabecera-comandas ion-padding-horizontal ion-padding-top">
      <p class="descripcion-comandas">
        Gestión de pedidos en cocina / depósito. Avance ítem por ítem o complete la comanda completa.
      </p>
    </div>

    <!-- Indicador de carga inicial -->
    <comp-esqueleto v-if="comandas_store.cargando && comandas_store.comandas.length === 0" />

    <!-- Error al cargar -->
    <comp-estado-error
      v-else-if="comandas_store.error"
      :mensaje="comandas_store.error"
      @reintentar="refrescar"
    />

    <!-- Estado vacío -->
    <div
      v-else-if="comandas_store.comandas.length === 0"
      class="ion-padding ion-text-center contenedor-vacio"
    >
      <ion-icon :icon="checkmarkDoneCircleOutline" class="icono-vacio" color="success"></ion-icon>
      <h3>¡Todo listo!</h3>
      <p>No hay pedidos pendientes de preparación en este momento.</p>
    </div>

    <!-- Lista de comandas -->
    <div v-else class="lista-comandas ion-padding">
      <ion-card
        v-for="comanda in comandas_store.comandas"
        :key="comanda.id"
        class="tarjeta-comanda vet-card"
        :class="{ 'comanda-lista': comanda.estado === 'listo' }"
        @click="abrirDetalle(comanda)"
      >
        <ion-card-header>
          <div class="cabecera-tarjeta">
            <div class="bloque-codigo">
              <ion-icon :icon="receiptOutline" color="primary"></ion-icon>
              <strong class="codigo-pedido">{{ comanda.codigo }}</strong>
              <ion-badge
                :color="comanda.tipo === 'entrega' ? 'tertiary' : 'medium'"
                class="badge-tipo"
              >
                {{ comanda.tipo === 'entrega' ? 'Delivery' : 'Mostrador' }}
              </ion-badge>
            </div>
            <ion-badge :color="obtenerColorBadge(comanda.estado)" class="badge-estado">
              {{ formatearEstado(comanda.estado) }}
            </ion-badge>
          </div>
        </ion-card-header>

        <ion-card-content>
          <div class="fila-cliente">
            <ion-icon :icon="personOutline" class="icono-dato" color="primary"></ion-icon>
            <strong>{{ comanda.cliente_nombre }}</strong>
          </div>

          <div v-if="comanda.notas" class="fila-notas ion-margin-top">
            <ion-icon :icon="documentTextOutline" class="icono-dato" color="medium"></ion-icon>
            <span class="texto-notas">"{{ comanda.notas }}"</span>
          </div>

          <!-- Barra de Progreso de Ítems Listos -->
          <div class="bloque-avance ion-margin-top">
            <div class="fila-texto-avance">
              <span class="etiqueta-avance">Progreso de ítems</span>
              <strong class="conteo-avance">
                {{ comanda.items_listos }} / {{ comanda.items_totales }} listos
              </strong>
            </div>
            <ion-progress-bar
              :value="comanda.items_totales > 0 ? (comanda.items_listos / comanda.items_totales) : 0"
              :color="comanda.items_listos === comanda.items_totales ? 'success' : 'primary'"
              class="barra-progreso"
            ></ion-progress-bar>
          </div>

          <div class="pie-tarjeta-comanda ion-margin-top">
            <small class="tiempo-comanda">
              {{ formatearHora(comanda.fecha_pedido || comanda.creado_en) }}
            </small>

            <ion-button
              size="small"
              shape="round"
              fill="outline"
              color="primary"
              @click.stop="abrirDetalle(comanda)"
            >
              <ion-icon slot="start" :icon="eyeOutline"></ion-icon>
              Ver ítems
            </ion-button>
          </div>
        </ion-card-content>
      </ion-card>
    </div>

    <!-- Modal de Detalle de Comanda -->
    <ion-modal :is-open="modalDetalleAbierto" class="modal-detalle-comanda" @didDismiss="modalDetalleAbierto = false">
      <ion-header>
        <ion-toolbar color="primary">
          <ion-title>{{ comandaSeleccionada?.codigo || 'Detalle de Comanda' }}</ion-title>
          <ion-buttons slot="end">
            <ion-button @click="modalDetalleAbierto = false">
              <ion-icon slot="icon-only" :icon="closeOutline"></ion-icon>
            </ion-button>
          </ion-buttons>
        </ion-toolbar>
      </ion-header>

      <ion-content v-if="comandaSeleccionada" class="ion-padding">
        <div class="resumen-modal-comanda vet-card ion-padding ion-margin-bottom">
          <div class="fila-cabecera-modal">
            <div>
              <h3>{{ comandaSeleccionada.cliente_nombre }}</h3>
              <p class="subtitulo-modal">
                {{ comandaSeleccionada.tipo === 'entrega' ? 'Envío a domicilio' : 'Retiro por mostrador' }} ·
                {{ formatearHora(comandaSeleccionada.fecha_pedido || comandaSeleccionada.creado_en) }}
              </p>
            </div>
            <ion-badge :color="obtenerColorBadge(comandaSeleccionada.estado)">
              {{ formatearEstado(comandaSeleccionada.estado) }}
            </ion-badge>
          </div>

          <div v-if="comandaSeleccionada.notas" class="notas-destacadas ion-margin-top">
            <strong>Notas:</strong> {{ comandaSeleccionada.notas }}
          </div>
        </div>

        <h4 class="titulo-seccion-items">Ítems a preparar</h4>

        <!-- Lista de ítems interactiva -->
        <ion-list class="lista-items-comanda">
          <ion-item
            v-for="item in comandaSeleccionada.items"
            :key="item.id"
            class="item-comanda-detalle"
            lines="full"
          >
            <div class="cuerpo-item-comanda">
              <div class="info-producto-item">
                <span class="cantidad-badge">{{ item.cantidad }}x</span>
                <span class="nombre-producto">{{ item.producto_nombre }}</span>
              </div>

              <div class="acciones-item-comanda">
                <ion-badge :color="obtenerColorBadgeItem(item.estado)" class="badge-item">
                  {{ formatearEstadoItem(item.estado) }}
                </ion-badge>

                <div class="grupo-botones-estado">
                  <ion-button
                    v-if="item.estado !== 'en_preparacion' && item.estado !== 'listo'"
                    size="small"
                    fill="outline"
                    color="secondary"
                    shape="round"
                    :disabled="guardandoItemId === item.id"
                    @click="cambiarEstadoItem(item, 'en_preparacion')"
                  >
                    <ion-spinner v-if="guardandoItemId === item.id" name="crescent" slot="start"></ion-spinner>
                    <ion-icon v-else slot="start" :icon="constructOutline"></ion-icon>
                    Preparar
                  </ion-button>

                  <ion-button
                    v-if="item.estado !== 'listo'"
                    size="small"
                    fill="solid"
                    color="success"
                    shape="round"
                    :disabled="guardandoItemId === item.id"
                    @click="cambiarEstadoItem(item, 'listo')"
                  >
                    <ion-icon slot="start" :icon="checkmarkCircleOutline"></ion-icon>
                    Listo
                  </ion-button>
                </div>
              </div>
            </div>
          </ion-item>
        </ion-list>

        <!-- Botón Inferior Destacado: Marcar todo listo -->
        <div class="botonera-inferior-comanda ion-margin-top ion-padding-bottom">
          <ion-button
            expand="block"
            shape="round"
            color="success"
            size="large"
            :disabled="marcandoTodoListo || comandaSeleccionada.estado === 'listo'"
            @click="marcarTodoListo(comandaSeleccionada)"
          >
            <ion-spinner v-if="marcandoTodoListo" name="crescent" slot="start"></ion-spinner>
            <ion-icon v-else slot="start" :icon="checkmarkDoneOutline"></ion-icon>
            {{ comandaSeleccionada.estado === 'listo' ? 'Comanda completa' : 'Marcar todo listo' }}
          </ion-button>
        </div>
      </ion-content>
    </ion-modal>
  </comp-page>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import {
  IonCard,
  IonCardHeader,
  IonCardContent,
  IonBadge,
  IonButton,
  IonIcon,
  IonProgressBar,
  IonModal,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonContent,
  IonList,
  IonItem,
  IonSpinner,
} from '@ionic/vue';
import {
  receiptOutline,
  personOutline,
  documentTextOutline,
  eyeOutline,
  closeOutline,
  checkmarkCircleOutline,
  checkmarkDoneOutline,
  checkmarkDoneCircleOutline,
  constructOutline,
} from 'ionicons/icons';
import CompPage from '@/components/estructura/comp_page.vue';
import CompEsqueleto from '@/components/base/comp_esqueleto.vue';
import CompEstadoError from '@/components/base/comp_estado_error.vue';
import { comandas_store } from '@/stores/comandas_store';
import { vibrar_toque, vibrar_error } from '@/services/vibracion_service';

const modalDetalleAbierto = ref(false);
const comandaSeleccionada = ref(null);
const guardandoItemId = ref(null);
const marcandoTodoListo = ref(false);

async function refrescar(event = null) {
  await comandas_store.cargar(true);
  if (comandaSeleccionada.value) {
    const act = comandas_store.comandas.find((c) => c.id === comandaSeleccionada.value.id);
    if (act) comandaSeleccionada.value = act;
  }
  if (event?.target?.complete) {
    event.target.complete();
  }
}

function abrirDetalle(comanda) {
  vibrar_toque();
  comandaSeleccionada.value = comanda;
  modalDetalleAbierto.value = true;
}

async function cambiarEstadoItem(item, nuevoEstado) {
  guardandoItemId.value = item.id;
  try {
    await comandas_store.mover_item(item.id, nuevoEstado, comandaSeleccionada.value.id);
  } catch (err) {
    console.error('Error al mover ítem:', err);
  } finally {
    guardandoItemId.value = null;
  }
}

async function marcarTodoListo(comanda) {
  marcandoTodoListo.value = true;
  try {
    await comandas_store.marcar_lista(comanda.id);
  } catch (err) {
    console.error('Error al marcar todo listo:', err);
  } finally {
    marcandoTodoListo.value = false;
  }
}

function formatearEstado(estado) {
  const map = {
    confirmado: 'Por preparar',
    en_preparacion: 'En preparación',
    listo: 'Listo para entrega',
  };
  return map[estado] || estado;
}

function obtenerColorBadge(estado) {
  const map = {
    confirmado: 'warning',
    en_preparacion: 'secondary',
    listo: 'success',
  };
  return map[estado] || 'medium';
}

function formatearEstadoItem(estado) {
  const map = {
    pendiente: 'Pendiente',
    en_preparacion: 'En preparación',
    listo: 'Listo',
  };
  return map[estado] || estado;
}

function obtenerColorBadgeItem(estado) {
  const map = {
    pendiente: 'medium',
    en_preparacion: 'secondary',
    listo: 'success',
  };
  return map[estado] || 'medium';
}

function formatearHora(fechaStr) {
  if (!fechaStr) return '';
  const d = new Date(fechaStr);
  return d.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' }) + ' hs';
}

onMounted(() => {
  comandas_store.cargar();
});
</script>

<style scoped>
.descripcion-comandas {
  font-size: 0.9rem;
  color: var(--ion-color-step-600, #475569);
  margin-top: 0;
}

.tarjeta-comanda {
  margin-bottom: 14px;
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
  border-left: 4px solid var(--ion-color-primary, #3880ff);
}

.tarjeta-comanda:active {
  transform: scale(0.99);
}

.tarjeta-comanda.comanda-lista {
  border-left-color: var(--ion-color-success, #2dd36f);
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

.codigo-pedido {
  font-size: 1.1rem;
}

.badge-tipo {
  font-size: 0.75rem;
  text-transform: uppercase;
}

.fila-cliente {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 1rem;
}

.fila-notas {
  display: flex;
  align-items: flex-start;
  gap: 8px;
}

.texto-notas {
  font-size: 0.85rem;
  color: var(--ion-color-step-600, #475569);
  font-style: italic;
}

.bloque-avance {
  background: var(--ion-color-step-50, #f8fafc);
  padding: 10px;
  border-radius: 8px;
}

.fila-texto-avance {
  display: flex;
  justify-content: space-between;
  font-size: 0.85rem;
  margin-bottom: 6px;
}

.barra-progreso {
  height: 8px;
  border-radius: 4px;
}

.pie-tarjeta-comanda {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.tiempo-comanda {
  color: var(--ion-color-step-500, #64748b);
  font-weight: 500;
}

.contenedor-vacio {
  margin-top: 40px;
}

.icono-vacio {
  font-size: 4rem;
  margin-bottom: 12px;
}

.modal-detalle-comanda {
  --border-radius: 16px;
}

.resumen-modal-comanda {
  border: 1px solid var(--ion-color-step-150, #e2e8f0);
}

.fila-cabecera-modal {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.fila-cabecera-modal h3 {
  margin: 0;
  font-size: 1.2rem;
  font-weight: 700;
}

.subtitulo-modal {
  margin: 4px 0 0 0;
  font-size: 0.85rem;
  color: var(--ion-color-step-500, #64748b);
}

.notas-destacadas {
  font-size: 0.9rem;
  background: var(--ion-color-warning-tint, #fef3c7);
  padding: 8px 12px;
  border-radius: 6px;
  color: var(--ion-color-warning-shade, #92400e);
}

.titulo-seccion-items {
  font-size: 1rem;
  font-weight: 700;
  margin: 16px 0 8px 0;
}

.lista-items-comanda {
  background: transparent;
}

.item-comanda-detalle {
  --background: var(--ion-card-background, #ffffff);
  --padding-start: 12px;
  --padding-end: 12px;
  --inner-padding-end: 0;
  margin-bottom: 8px;
  border-radius: 10px;
  border: 1px solid var(--ion-color-step-150, #e2e8f0);
}

.cuerpo-item-comanda {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 10px 0;
}

.info-producto-item {
  display: flex;
  align-items: center;
  gap: 10px;
}

.cantidad-badge {
  background: var(--ion-color-primary, #3880ff);
  color: #ffffff;
  font-weight: 700;
  font-size: 0.85rem;
  padding: 2px 8px;
  border-radius: 12px;
}

.nombre-producto {
  font-weight: 600;
  font-size: 1rem;
}

.acciones-item-comanda {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.badge-item {
  font-size: 0.8rem;
  padding: 4px 8px;
}

.grupo-botones-estado {
  display: flex;
  gap: 6px;
}
</style>
