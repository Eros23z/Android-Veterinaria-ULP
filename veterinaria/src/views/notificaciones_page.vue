<template>
  <comp-page titulo="Notificaciones" :mostrar_actualizar="true" @actualizar="refrescar">
    <template #acciones>
      <ion-button
        v-if="notificaciones_store.no_leidas > 0"
        fill="clear"
        size="small"
        :disabled="marcandoTodas"
        @click="marcarTodas"
      >
        <ion-icon slot="start" :icon="checkmarkDoneOutline"></ion-icon>
        <span class="texto-accion-header">Marcar leídas</span>
      </ion-button>
    </template>

    <!-- Resumen y botón superior -->
    <div class="seccion-cabecera ion-padding">
      <div class="fila-resumen">
        <div>
          <h2 class="titulo-bandeja">Bandeja de Entrada</h2>
          <p class="subtitulo-bandeja">
            <span v-if="notificaciones_store.no_leidas > 0" class="texto-no-leidas">
              {{ notificaciones_store.no_leidas }} aviso(s) sin leer
            </span>
            <span v-else class="texto-al-dia">
              Estás al día con todos tus avisos
            </span>
          </p>
        </div>

        <ion-button
          v-if="notificaciones_store.no_leidas > 0"
          size="small"
          shape="round"
          fill="outline"
          color="primary"
          :disabled="marcandoTodas"
          @click="marcarTodas"
        >
          <ion-spinner v-if="marcandoTodas" name="crescent" slot="start"></ion-spinner>
          <ion-icon v-else slot="start" :icon="checkmarkDoneOutline"></ion-icon>
          Marcar todas
        </ion-button>
      </div>
    </div>

    <!-- Indicador de carga inicial -->
    <comp-esqueleto v-if="notificaciones_store.cargando && notificaciones_store.notificaciones.length === 0" />

    <!-- Estado vacío -->
    <div
      v-else-if="notificaciones_store.notificaciones.length === 0"
      class="ion-padding ion-text-center contenedor-vacio"
    >
      <ion-icon :icon="notificationsOffOutline" class="icono-vacio"></ion-icon>
      <h3>Sin notificaciones</h3>
      <p>No tienes notificaciones pendientes ni mensajes anteriores.</p>
    </div>

    <!-- Lista paginada de notificaciones -->
    <comp-lista
      v-else
      :total="notificaciones_store.paginacion.total"
      :cantidad_mostrada="notificaciones_store.notificaciones.length"
      :hay_mas="notificaciones_store.paginacion.hay_mas"
      :cargando="notificaciones_store.cargando"
      @ver_mas="cargarMas"
    >
      <template #lista>
        <ion-list lines="inset" class="lista-notificaciones">
          <ion-item
            v-for="notif in notificaciones_store.notificaciones"
            :key="notif.id"
            button
            :detail="false"
            class="item-notificacion"
            :class="{ 'item-no-leido': !notif.es_leida }"
            @click="tocarNotificacion(notif)"
          >
            <ion-icon
              slot="start"
              :icon="obtenerIconoTipo(notif.tipo)"
              :color="obtenerColorTipo(notif.tipo, notif.es_leida)"
              class="icono-tipo-notif"
            ></ion-icon>

            <ion-label class="ion-text-wrap">
              <div class="fila-titulo">
                <h3 class="titulo-notif" :class="{ 'texto-negrita': !notif.es_leida }">
                  {{ notif.titulo }}
                </h3>
                <ion-badge v-if="!notif.es_leida" color="danger" class="badge-nueva">
                  NUEVA
                </ion-badge>
              </div>

              <p class="cuerpo-mensaje">{{ notif.mensaje }}</p>

              <div class="pie-notif">
                <span class="fecha-notif">{{ formatearFecha(notif.creado_en) }}</span>
                <span v-if="notif.es_leida" class="indicador-leida">
                  <ion-icon :icon="checkmarkOutline" class="icono-check"></ion-icon> Leída
                </span>
              </div>
            </ion-label>
          </ion-item>
        </ion-list>
      </template>
    </comp-lista>
  </comp-page>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import {
  IonList,
  IonItem,
  IonLabel,
  IonButton,
  IonIcon,
  IonBadge,
  IonSpinner,
} from '@ionic/vue';
import {
  notificationsOffOutline,
  checkmarkDoneOutline,
  checkmarkOutline,
  informationCircleOutline,
  receiptOutline,
  navigateOutline,
} from 'ionicons/icons';
import compPage from '@/components/estructura/comp_page.vue';
import compEsqueleto from '@/components/base/comp_esqueleto.vue';
import compLista from '@/components/base/comp_lista.vue';
import { notificaciones_store } from '@/stores/notificaciones_store';
import { vibrar_toque, vibrar_error } from '@/services/vibracion_service';

const marcandoTodas = ref(false);

async function refrescar(event = null) {
  await notificaciones_store.cargar(true);
  if (event?.target?.complete) {
    event.target.complete();
  }
}

async function cargarMas() {
  await notificaciones_store.cargar_mas();
}

async function tocarNotificacion(notif) {
  if (!notif.es_leida) {
    try {
      await vibrar_toque();
      await notificaciones_store.marcar_leida(notif.id);
    } catch (err) {
      await vibrar_error();
    }
  }
}

async function marcarTodas() {
  marcandoTodas.value = true;
  try {
    await vibrar_toque();
    await notificaciones_store.marcar_todas_leidas();
  } catch (err) {
    await vibrar_error();
  } finally {
    marcandoTodas.value = false;
  }
}

function obtenerIconoTipo(tipo) {
  const t = String(tipo).toLowerCase();
  if (t === 'pedido') return receiptOutline;
  if (t === 'entrega') return navigateOutline;
  return informationCircleOutline;
}

function obtenerColorTipo(tipo, esLeida) {
  if (esLeida) return 'medium';
  const t = String(tipo).toLowerCase();
  if (t === 'pedido') return 'primary';
  if (t === 'entrega') return 'success';
  return 'secondary';
}

function formatearFecha(fechaStr) {
  if (!fechaStr) return '';
  const fecha = new Date(fechaStr);
  return fecha.toLocaleString('es-AR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

onMounted(() => {
  notificaciones_store.cargar(true);
});
</script>

<style scoped>
.seccion-cabecera {
  background: var(--ion-color-step-50, #f8fafc);
  border-bottom: 1px solid var(--ion-color-step-150, #e2e8f0);
}

.fila-resumen {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.titulo-bandeja {
  font-size: 1.15rem;
  font-weight: 700;
  margin: 0;
  color: var(--ion-color-step-900, #0f172a);
}

.subtitulo-bandeja {
  font-size: 0.85rem;
  margin: 3px 0 0;
}

.texto-no-leidas {
  color: var(--ion-color-danger, #ef4444);
  font-weight: 600;
}

.texto-al-dia {
  color: var(--ion-color-success, #10b981);
  font-weight: 500;
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

.lista-notificaciones {
  padding: 0;
}

.item-notificacion {
  --padding-top: 12px;
  --padding-bottom: 12px;
  transition: background-color 0.2s ease;
}

.item-no-leido {
  --background: rgba(var(--ion-color-primary-rgb, 56, 128, 255), 0.06);
  border-left: 4px solid var(--ion-color-primary, #3880ff);
}

.icono-tipo-notif {
  font-size: 24px;
  margin-top: 2px;
}

.fila-titulo {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.titulo-notif {
  font-size: 0.98rem;
  color: var(--ion-color-step-900, #0f172a);
  margin: 0;
}

.texto-negrita {
  font-weight: 700;
}

.badge-nueva {
  font-size: 10px;
  letter-spacing: 0.5px;
  padding: 2px 6px;
  border-radius: 6px;
}

.cuerpo-mensaje {
  font-size: 0.88rem;
  color: var(--ion-color-step-700, #334155);
  margin: 4px 0 6px;
  line-height: 1.35;
}

.pie-notif {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 0.78rem;
  color: var(--ion-color-medium, #94a3b8);
}

.indicador-leida {
  display: inline-flex;
  align-items: center;
  gap: 3px;
}

.icono-check {
  font-size: 13px;
}

.texto-accion-header {
  font-size: 0.85rem;
}
</style>
