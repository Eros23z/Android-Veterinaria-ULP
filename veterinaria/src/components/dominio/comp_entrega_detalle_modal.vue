<template>
  <ion-modal :is-open="is_open" class="modal-detalle-entrega" @didDismiss="cerrar">
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>{{ entrega?.codigo_pedido || 'Detalle de Entrega' }}</ion-title>
        <ion-buttons slot="end">
          <ion-button @click="cerrar">
            <ion-icon slot="icon-only" :icon="closeOutline"></ion-icon>
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content v-if="entrega" class="ion-padding">
      <div class="tarjeta-info-entrega vet-card ion-padding ion-margin-bottom">
        <div class="fila-cabecera-modal">
          <div>
            <h2 class="codigo-titulo">{{ entrega.codigo_pedido }}</h2>
            <p class="fecha-subtitulo">
              Pedido del {{ formatearFecha(entrega.fecha_pedido || entrega.creado_en) }}
            </p>
          </div>
          <ion-badge :color="obtenerColorBadge(entrega.estado)" class="badge-estado">
            {{ formatearEstado(entrega.estado) }}
          </ion-badge>
        </div>

        <div class="divisor-suave"></div>

        <div class="seccion-datos">
          <div class="fila-dato">
            <ion-icon :icon="personOutline" class="icono-dato" color="primary"></ion-icon>
            <div>
              <strong>{{ entrega.cliente_nombre }}</strong>
              <div v-if="entrega.cliente_telefono">
                <a :href="`tel:${entrega.cliente_telefono}`" class="enlace-telefono">
                  <ion-icon :icon="callOutline" class="icono-mini"></ion-icon>
                  {{ entrega.cliente_telefono }}
                </a>
              </div>
            </div>
          </div>

          <div class="fila-dato ion-margin-top">
            <ion-icon :icon="locationOutline" class="icono-dato" color="primary"></ion-icon>
            <div>
              <span>{{ entrega.direccion_linea || entrega.cliente_direccion || 'Sin dirección' }}</span>
              <div v-if="entrega.direccion_referencia" class="texto-referencia">
                Ref: {{ entrega.direccion_referencia }}
              </div>
            </div>
          </div>

          <div class="fila-dato ion-margin-top">
            <ion-icon :icon="cashOutline" class="icono-dato" color="success"></ion-icon>
            <div>
              <span class="monto-modal">Total: ${{ Number(entrega.total || 0).toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}</span>
            </div>
          </div>

          <div v-if="entrega.observaciones" class="fila-dato ion-margin-top">
            <ion-icon :icon="documentTextOutline" class="icono-dato" color="medium"></ion-icon>
            <div class="texto-obs">
              <em>{{ entrega.observaciones }}</em>
            </div>
          </div>
        </div>
      </div>

      <!-- Sección de Firma de Recepción / Conformidad -->
      <div
        v-if="entrega.estado === 'en_camino' || entrega.estado === 'entregada'"
        class="seccion-firma-conformidad vet-card ion-padding"
      >
        <div class="cabecera-firma">
          <div class="titulo-con-icono">
            <ion-icon :icon="createOutline" color="primary"></ion-icon>
            <h3>Firma de Conformidad</h3>
          </div>
          <ion-badge v-if="entrega.firma_url" color="success">Registrada</ion-badge>
          <ion-badge v-else color="warning">Pendiente</ion-badge>
        </div>

        <!-- 1. Si ya tiene firma registrada: mostrar la imagen -->
        <div v-if="entrega.firma_url" class="visor-firma ion-margin-top">
          <p class="descripcion-firma">Recepción firmada por el destinatario:</p>
          <div class="marco-imagen-firma">
            <img :src="resolverUrlFirma(entrega.firma_url)" alt="Firma de recepción" />
          </div>
        </div>

        <!-- 2. Si no tiene firma: permitir firmar -->
        <div v-else class="bloque-firma-nueva ion-margin-top">
          <div v-if="!mostrandoLienzo">
            <p class="descripcion-firma">
              El cliente o receptor debe rubricar en pantalla al recibir el pedido.
            </p>
            <ion-button
              expand="block"
              shape="round"
              color="primary"
              @click="mostrandoLienzo = true"
            >
              <ion-icon slot="start" :icon="pencilOutline"></ion-icon>
              Firmar recepción
            </ion-button>
          </div>

          <div v-else class="formulario-firma">
            <comp-firma-entrega-canvas ref="firmaCanvasRef" />

            <div class="botones-accion-firma ion-margin-top">
              <ion-button
                fill="outline"
                color="medium"
                shape="round"
                :disabled="guardandoFirma"
                @click="mostrandoLienzo = false"
              >
                Cancelar
              </ion-button>

              <ion-button
                shape="round"
                color="success"
                :disabled="guardandoFirma"
                @click="confirmarGuardadoFirma"
              >
                <ion-spinner v-if="guardandoFirma" name="crescent" slot="start"></ion-spinner>
                <ion-icon v-else slot="start" :icon="checkmarkCircleOutline"></ion-icon>
                Guardar firma
              </ion-button>
            </div>
          </div>
        </div>
      </div>
    </ion-content>
  </ion-modal>
</template>

<script setup>
import { ref } from 'vue';
import {
  IonModal,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonButton,
  IonContent,
  IonBadge,
  IonIcon,
  IonSpinner,
} from '@ionic/vue';
import {
  closeOutline,
  personOutline,
  callOutline,
  locationOutline,
  cashOutline,
  documentTextOutline,
  createOutline,
  pencilOutline,
  checkmarkCircleOutline,
} from 'ionicons/icons';
import CompFirmaEntregaCanvas from '@/components/base/comp_firma_entrega_canvas.vue';
import { entregas_service } from '@/services/entregas_service';
import { vibrar_toque, vibrar_exito, vibrar_error } from '@/services/vibracion_service';
import { obtener_api_url } from '@/config/debug';

const props = defineProps({
  is_open: { type: Boolean, default: false },
  entrega: { type: Object, default: () => null },
});

const emit = defineEmits(['cerrar', 'actualizado']);

const mostrandoLienzo = ref(false);
const guardandoFirma = ref(false);
const firmaCanvasRef = ref(null);

function cerrar() {
  mostrandoLienzo.value = false;
  emit('cerrar');
}

function resolverUrlFirma(url) {
  if (!url) return '';
  if (url.startsWith('http://') || url.startsWith('https://')) return url;
  const base = obtener_api_url();
  return `${base}${url.startsWith('/') ? '' : '/'}${url}`;
}

function formatearFecha(fechaStr) {
  if (!fechaStr) return '';
  const d = new Date(fechaStr);
  return d.toLocaleDateString('es-AR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatearEstado(estado) {
  const map = {
    pendiente: 'Pendiente',
    asignada: 'Asignada',
    en_camino: 'En Camino',
    entregada: 'Entregada',
    cancelada: 'Cancelada',
  };
  return map[estado] || estado;
}

function obtenerColorBadge(estado) {
  const map = {
    pendiente: 'warning',
    asignada: 'tertiary',
    en_camino: 'secondary',
    entregada: 'success',
    cancelada: 'danger',
  };
  return map[estado] || 'medium';
}

async function confirmarGuardadoFirma() {
  if (!firmaCanvasRef.value) return;

  const archivo = await firmaCanvasRef.value.obtener_archivo();
  if (!archivo) {
    await vibrar_error();
    alert('Por favor realice su firma en el panel antes de guardar.');
    return;
  }

  guardandoFirma.value = true;
  await vibrar_toque();

  try {
    const res = await entregas_service.guardar_firma_entrega(props.entrega.id, archivo);
    await vibrar_exito();
    mostrandoLienzo.value = false;
    emit('actualizado', { id: props.entrega.id, firma_url: res.firma_url });
  } catch (error) {
    console.error('Error al guardar firma:', error);
    await vibrar_error();
    alert(error.message || 'No se pudo guardar la firma de recepción.');
  } finally {
    guardandoFirma.value = false;
  }
}
</script>

<style scoped>
.modal-detalle-entrega {
  --border-radius: 16px;
}

.tarjeta-info-entrega {
  background: var(--ion-card-background, #ffffff);
  border-radius: 12px;
  border: 1px solid var(--ion-color-step-150, #e2e8f0);
}

.fila-cabecera-modal {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.codigo-titulo {
  font-size: 1.25rem;
  font-weight: 700;
  margin: 0;
  color: var(--ion-text-color, #0f172a);
}

.fecha-subtitulo {
  font-size: 0.85rem;
  color: var(--ion-color-step-500, #64748b);
  margin: 4px 0 0 0;
}

.divisor-suave {
  height: 1px;
  background: var(--ion-color-step-150, #e2e8f0);
  margin: 12px 0;
}

.seccion-datos {
  display: flex;
  flex-direction: column;
}

.fila-dato {
  display: flex;
  align-items: flex-start;
  gap: 10px;
}

.icono-dato {
  font-size: 1.2rem;
  margin-top: 2px;
  flex-shrink: 0;
}

.icono-mini {
  font-size: 0.9rem;
  vertical-align: middle;
  margin-right: 4px;
}

.enlace-telefono {
  color: var(--ion-color-primary, #3880ff);
  text-decoration: none;
  font-size: 0.9rem;
}

.texto-referencia {
  font-size: 0.85rem;
  color: var(--ion-color-step-500, #64748b);
  margin-top: 2px;
}

.monto-modal {
  font-weight: 700;
  font-size: 1.05rem;
  color: var(--ion-color-success-shade, #15803d);
}

.texto-obs {
  font-size: 0.9rem;
  color: var(--ion-color-step-600, #475569);
}

.seccion-firma-conformidad {
  background: var(--ion-card-background, #ffffff);
  border-radius: 12px;
  border: 1px solid var(--ion-color-step-150, #e2e8f0);
}

.cabecera-firma {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.titulo-con-icono {
  display: flex;
  align-items: center;
  gap: 8px;
}

.titulo-con-icono h3 {
  font-size: 1.05rem;
  font-weight: 600;
  margin: 0;
}

.descripcion-firma {
  font-size: 0.875rem;
  color: var(--ion-color-step-600, #475569);
  margin-bottom: 12px;
}

.marco-imagen-firma {
  background: #ffffff;
  border: 1px solid var(--ion-color-step-300, #cbd5e1);
  border-radius: 8px;
  padding: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  max-height: 160px;
}

.marco-imagen-firma img {
  max-width: 100%;
  max-height: 130px;
  object-fit: contain;
}

.botones-accion-firma {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
