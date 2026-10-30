<template>
  <div class="contenedor-firma-canvas">
    <div class="marco-canvas">
      <canvas
        ref="lienzoRef"
        class="lienzo-firma"
        @pointerdown="empezar"
        @pointermove="mover"
        @pointerup="terminar"
        @pointerleave="terminar"
        @pointercancel="terminar"
      ></canvas>

      <div v-if="!tieneTrazos" class="marca-agua-firma">
        <span>Firme aquí</span>
      </div>

      <div class="linea-guia-firma"></div>
    </div>

    <div class="barra-acciones-firma">
      <ion-button
        size="small"
        fill="outline"
        color="medium"
        shape="round"
        :disabled="!tieneTrazos"
        @click="limpiar"
      >
        <ion-icon slot="start" :icon="trashOutline"></ion-icon>
        Borrar firma
      </ion-button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue';
import { IonButton, IonIcon } from '@ionic/vue';
import { trashOutline } from 'ionicons/icons';

const lienzoRef = ref(null);
const dibujando = ref(false);
const tieneTrazos = ref(false);
let ctx = null;
let resizeObserver = null;
let trazosGuardados = [];

function empezar(e) {
  if (!ctx || !lienzoRef.value) return;
  lienzoRef.value.setPointerCapture(e.pointerId);
  dibujando.value = true;
  const pos = obtenerPosicion(e);
  ctx.beginPath();
  ctx.moveTo(pos.x, pos.y);
  trazosGuardados.push([{ x: pos.x, y: pos.y }]);
}

function mover(e) {
  if (!dibujando.value || !ctx) return;
  const pos = obtenerPosicion(e);
  ctx.lineTo(pos.x, pos.y);
  ctx.stroke();
  tieneTrazos.value = true;
  if (trazosGuardados.length > 0) {
    trazosGuardados[trazosGuardados.length - 1].push({ x: pos.x, y: pos.y });
  }
}

function terminar(e) {
  if (!dibujando.value) return;
  dibujando.value = false;
  if (lienzoRef.value && e.pointerId) {
    try {
      lienzoRef.value.releasePointerCapture(e.pointerId);
    } catch (_) {}
  }
}

function obtenerPosicion(e) {
  const rect = lienzoRef.value.getBoundingClientRect();
  return {
    x: e.clientX - rect.left,
    y: e.clientY - rect.top,
  };
}

function configurarContexto() {
  if (!ctx) return;
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  ctx.lineWidth = 2.5;
  ctx.strokeStyle = '#1e293b';
}

function redimensionar() {
  if (!lienzoRef.value) return;
  const canvas = lienzoRef.value;
  const rect = canvas.getBoundingClientRect();
  if (rect.width === 0 || rect.height === 0) return;

  const dpr = window.devicePixelRatio || 1;
  canvas.width = rect.width * dpr;
  canvas.height = rect.height * dpr;

  ctx = canvas.getContext('2d');
  ctx.scale(dpr, dpr);
  configurarContexto();

  // Redibujar trazos si hubo cambio de tamaño
  if (trazosGuardados.length > 0) {
    for (const trazo of trazosGuardados) {
      if (trazo.length === 0) continue;
      ctx.beginPath();
      ctx.moveTo(trazo[0].x, trazo[0].y);
      for (let i = 1; i < trazo.length; i++) {
        ctx.lineTo(trazo[i].x, trazo[i].y);
      }
      ctx.stroke();
    }
  }
}

function limpiar() {
  if (!ctx || !lienzoRef.value) return;
  const canvas = lienzoRef.value;
  const dpr = window.devicePixelRatio || 1;
  ctx.clearRect(0, 0, canvas.width / dpr, canvas.height / dpr);
  trazosGuardados = [];
  tieneTrazos.value = false;
}

/**
 * Genera un File PNG con la firma capturada.
 * @returns {Promise<File|null>}
 */
function obtener_archivo() {
  return new Promise((resolve) => {
    if (!lienzoRef.value || !tieneTrazos.value) {
      resolve(null);
      return;
    }

    const canvas = lienzoRef.value;
    // Crear un canvas temporal con fondo blanco para exportar PNG nítido
    const exportCanvas = document.createElement('canvas');
    exportCanvas.width = canvas.width;
    exportCanvas.height = canvas.height;
    const expCtx = exportCanvas.getContext('2d');
    expCtx.fillStyle = '#ffffff';
    expCtx.fillRect(0, 0, exportCanvas.width, exportCanvas.height);
    expCtx.drawImage(canvas, 0, 0);

    exportCanvas.toBlob((blob) => {
      if (!blob) {
        resolve(null);
        return;
      }
      const archivo = new File([blob], 'firma.png', { type: 'image/png' });
      resolve(archivo);
    }, 'image/png');
  });
}

onMounted(() => {
  redimensionar();
  if (typeof ResizeObserver !== 'undefined' && lienzoRef.value) {
    resizeObserver = new ResizeObserver(() => redimensionar());
    resizeObserver.observe(lienzoRef.value);
  }
});

onUnmounted(() => {
  if (resizeObserver) {
    resizeObserver.disconnect();
  }
});

defineExpose({
  limpiar,
  obtener_archivo,
  tiene_trazos: tieneTrazos,
});
</script>

<style scoped>
.contenedor-firma-canvas {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.marco-canvas {
  position: relative;
  width: 100%;
  height: 200px;
  background: #ffffff;
  border: 2px dashed var(--ion-color-step-300, #cbd5e1);
  border-radius: 12px;
  overflow: hidden;
  box-shadow: inset 0 2px 4px rgba(0, 0, 0, 0.05);
}

.lienzo-firma {
  width: 100%;
  height: 100%;
  background: #ffffff;
  touch-action: none;
  cursor: crosshair;
  display: block;
}

.marca-agua-firma {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  color: var(--ion-color-step-400, #94a3b8);
  font-size: 1.1rem;
  font-weight: 500;
  pointer-events: none;
  user-select: none;
}

.linea-guia-firma {
  position: absolute;
  bottom: 30px;
  left: 20px;
  right: 20px;
  height: 1px;
  background: var(--ion-color-step-200, #e2e8f0);
  pointer-events: none;
}

.barra-acciones-firma {
  display: flex;
  justify-content: flex-end;
}
</style>
