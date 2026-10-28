<template>
  <ion-modal :is-open="abierto" class="modal-selector-mapa" @didDismiss="cerrarModal" @didPresent="alAbrirModal">
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>{{ titulo || 'Seleccionar ubicación en el mapa' }}</ion-title>
        <ion-buttons slot="end">
          <ion-button @click="cerrarModal">
            <ion-icon slot="icon-only" :icon="closeOutline"></ion-icon>
          </ion-button>
        </ion-buttons>
      </ion-toolbar>

      <!-- Barra de búsqueda de direcciones integrada -->
      <ion-toolbar class="toolbar-buscador">
        <div class="buscador-mapa-fila">
          <ion-searchbar
            v-model="textoBusqueda"
            placeholder="Buscar calle, localidad..."
            :debounce="400"
            @ionInput="onInputBusqueda"
            @keyup.enter="ejecutarBusqueda"
            class="input-buscador-mapa"
          ></ion-searchbar>
          <ion-button
            fill="solid"
            color="secondary"
            size="small"
            class="boton-buscar-mapa"
            :disabled="buscandoDireccion"
            @click="ejecutarBusqueda"
          >
            <ion-spinner v-if="buscandoDireccion" name="crescent" class="spinner-sm"></ion-spinner>
            <ion-icon v-else slot="icon-only" :icon="searchOutline"></ion-icon>
          </ion-button>
        </div>

        <!-- Lista de sugerencias de direcciones encontradas -->
        <div v-if="sugerencias.length > 0" class="lista-sugerencias-mapa">
          <div
            v-for="(sug, idx) in sugerencias"
            :key="idx"
            class="item-sugerencia"
            @click="seleccionarSugerencia(sug)"
          >
            <ion-icon :icon="locationOutline" class="icono-sug"></ion-icon>
            <div class="texto-sug">
              <strong>{{ sug.direccion }}</strong>
              <small>{{ sug.nombre }}</small>
            </div>
          </div>
        </div>
      </ion-toolbar>
    </ion-header>

    <ion-content :scrollY="false" class="contenido-mapa">
      <!-- Aviso si las imágenes de OpenStreetMap no pudieron cargar -->
      <div v-if="mapa_caido" class="aviso-caida-flotante ion-padding">
        <comp-aviso
          tono="advertencia"
          titulo="Mapa no disponible sin conexión"
          mensaje="No se pudieron cargar los mosaicos de OpenStreetMap. Podés confirmar las coordenadas capturadas o ingresar la dirección manualmente."
        />
      </div>

      <!-- Contenedor del Mapa interactivo Web Mercator -->
      <div
        ref="contenedorMapaRef"
        class="contenedor-mapa"
        @pointerdown="onPointerDown"
        @pointermove="onPointerMove"
        @pointerup="onPointerUp"
        @pointercancel="onPointerUp"
        @wheel.prevent="onWheel"
      >
        <!-- Grilla de Mosaicos (Tiles) de OSM -->
        <div class="capa-tiles">
          <img
            v-for="tile in tilesVisibles"
            :key="tile.key"
            :src="tile.url"
            :alt="tile.key"
            class="tile-imagen"
            :style="{
              left: `${tile.x}px`,
              top: `${tile.y}px`,
              width: `${TILE_SIZE}px`,
              height: `${TILE_SIZE}px`,
            }"
            loading="eager"
            @load="onTileCargada"
            @error="onTileError"
          />
        </div>

        <!-- Marcador Pin Fijo en el centro de la pantalla -->
        <div class="pin-marcador-centro">
          <div class="pin-icono-envoltorio">
            <ion-icon :icon="location" class="pin-icono"></ion-icon>
          </div>
          <div class="pin-sombra"></div>
        </div>

        <!-- Atribución obligatoria de OpenStreetMap -->
        <div class="atribucion-osm">
          <span>© <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a></span>
        </div>

        <!-- Controles flotantes de Zoom y GPS -->
        <div class="controles-mapa-flotantes">
          <ion-button
            shape="round"
            color="light"
            size="small"
            class="boton-ctrl-mapa"
            title="Acercar"
            @click.stop="ajustarZoom(1)"
          >
            <ion-icon slot="icon-only" :icon="addOutline"></ion-icon>
          </ion-button>
          <ion-button
            shape="round"
            color="light"
            size="small"
            class="boton-ctrl-mapa"
            title="Alejar"
            @click.stop="ajustarZoom(-1)"
          >
            <ion-icon slot="icon-only" :icon="removeOutline"></ion-icon>
          </ion-button>
          <ion-button
            shape="round"
            color="primary"
            size="small"
            class="boton-ctrl-mapa"
            title="Centrar en mi ubicación GPS"
            :disabled="obteniendoGps"
            @click.stop="centrarEnGpsActual"
          >
            <ion-spinner v-if="obteniendoGps" name="crescent" class="spinner-sm"></ion-spinner>
            <ion-icon v-else slot="icon-only" :icon="navigateOutline"></ion-icon>
          </ion-button>
        </div>
      </div>

      <!-- Barra Inferior de Información y Confirmación -->
      <div class="barra-confirmacion-mapa">
        <div class="info-punto-actual">
          <div class="direccion-resumida">
            <ion-icon :icon="locationOutline" color="primary"></ion-icon>
            <span v-if="cargandoDireccionInversa" class="texto-cargando">Resolviendo dirección...</span>
            <span v-else class="texto-direccion">{{ direccionActual || 'Punto en el mapa' }}</span>
          </div>
          <div class="coordenadas-detalle">
            <small>Lat: {{ centroLat.toFixed(6) }} · Lng: {{ centroLng.toFixed(6) }} (Zoom {{ zoom }})</small>
          </div>
        </div>

        <div class="acciones-inferiores">
          <ion-button
            expand="block"
            color="success"
            shape="round"
            class="boton-confirmar-ubicacion"
            @click="confirmarPunto"
          >
            <ion-icon slot="start" :icon="checkmarkCircleOutline"></ion-icon>
            Usar este punto
          </ion-button>
        </div>
      </div>
    </ion-content>
  </ion-modal>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue';
import {
  IonModal,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonButton,
  IonIcon,
  IonContent,
  IonSearchbar,
  IonSpinner,
} from '@ionic/vue';
import {
  closeOutline,
  searchOutline,
  locationOutline,
  location,
  addOutline,
  removeOutline,
  navigateOutline,
  checkmarkCircleOutline,
} from 'ionicons/icons';
import compAviso from '@/components/base/comp_aviso.vue';
import {
  obtener_ubicacion_actual,
  buscar_direcciones,
  buscar_direccion_de_punto,
} from '@/services/geolocalizacion_service';
import { vibracion_service } from '@/services/vibracion_service';

const props = defineProps({
  abierto: { type: Boolean, default: false },
  titulo: { type: String, default: 'Seleccionar ubicación en el mapa' },
  latitud: { type: [Number, String], default: null },
  longitud: { type: [Number, String], default: null },
  direccion: { type: String, default: '' },
});

const emit = defineEmits(['confirmar', 'cerrar']);

// Constantes de Web Mercator
const TILE_SIZE = 256;
const ZOOM_MIN = 3;
const ZOOM_MAX = 19;

// Coordenadas iniciales por defecto (San Luis / Merlo, Argentina)
const DEFAULT_LAT = -33.2996;
const DEFAULT_LNG = -66.3378;

const centroLat = ref(DEFAULT_LAT);
const centroLng = ref(DEFAULT_LNG);
const zoom = ref(16);

const anchoMapa = ref(400);
const altoMapa = ref(500);
const contenedorMapaRef = ref(null);

const tiles_cargadas = ref(0);
const tiles_con_error = ref(0);
const mapa_caido = ref(false);

const direccionActual = ref('');
const direccion_elegida = ref(false);
const cargandoDireccionInversa = ref(false);
const textoBusqueda = ref('');
const buscandoDireccion = ref(false);
const sugerencias = ref([]);
const obteniendoGps = ref(false);

let timerGeocodificacion = null;
let resizeObserver = null;

// Punteros activos para soporte multitouch / pellizco
const punterosActivos = new Map();
let distanciaInicialPinch = 0;
let zoomInicialPinch = 16;

// ----------------------------------------------------
// Matemáticas Web Mercator (EPSG:3857)
// ----------------------------------------------------
function a_pixel(lat, lng, z) {
  const escala = TILE_SIZE * Math.pow(2, z);
  const x = ((lng + 180) / 360) * escala;
  const sinLat = Math.sin((lat * Math.PI) / 180);
  const clampSinLat = Math.max(-0.9999, Math.min(0.9999, sinLat));
  const y = (0.5 - Math.log((1 + clampSinLat) / (1 - clampSinLat)) / (4 * Math.PI)) * escala;
  return { x, y };
}

function a_grados(x, y, z) {
  const escala = TILE_SIZE * Math.pow(2, z);
  const lng = (x / escala) * 360 - 180;
  const n = Math.PI - (2 * Math.PI * y) / escala;
  const lat = (180 / Math.PI) * Math.atan(0.5 * (Math.exp(n) - Math.exp(-n)));
  return { lat, lng };
}

// ----------------------------------------------------
// Cálculo dinámico de Tiles visibles
// ----------------------------------------------------
const tilesVisibles = computed(() => {
  const w = anchoMapa.value || 360;
  const h = altoMapa.value || 480;
  const z = zoom.value;

  const centroPix = a_pixel(centroLat.value, centroLng.value, z);

  const minX = centroPix.x - w / 2;
  const maxX = centroPix.x + w / 2;
  const minY = centroPix.y - h / 2;
  const maxY = centroPix.y + h / 2;

  const numTiles = Math.pow(2, z);

  const minTileX = Math.floor(minX / TILE_SIZE);
  const maxTileX = Math.floor(maxX / TILE_SIZE);
  const minTileY = Math.max(0, Math.floor(minY / TILE_SIZE));
  const maxTileY = Math.min(numTiles - 1, Math.floor(maxY / TILE_SIZE));

  const tiles = [];
  for (let tx = minTileX; tx <= maxTileX; tx++) {
    for (let ty = minTileY; ty <= maxTileY; ty++) {
      const wrapX = ((tx % numTiles) + numTiles) % numTiles;
      const posX = tx * TILE_SIZE - minX;
      const posY = ty * TILE_SIZE - minY;

      tiles.push({
        key: `${z}-${wrapX}-${ty}`,
        url: `https://tile.openstreetmap.org/${z}/${wrapX}/${ty}.png`,
        x: Math.round(posX),
        y: Math.round(posY),
      });
    }
  }

  return tiles;
});

function onTileCargada() {
  tiles_cargadas.value++;
}

function onTileError() {
  tiles_con_error.value++;
  if (tiles_cargadas.value === 0 && tiles_con_error.value >= 4) {
    mapa_caido.value = true;
  }
}

// ----------------------------------------------------
// Gestión de gestos táctiles (Drag y Pinch-to-zoom)
// ----------------------------------------------------
function onPointerDown(e) {
  if (e.target && e.target.setPointerCapture) {
    try {
      e.target.setPointerCapture(e.pointerId);
    } catch {}
  }
  punterosActivos.set(e.pointerId, { x: e.clientX, y: e.clientY });

  if (punterosActivos.size === 2) {
    const pts = Array.from(punterosActivos.values());
    distanciaInicialPinch = Math.hypot(pts[0].x - pts[1].x, pts[0].y - pts[1].y);
    zoomInicialPinch = zoom.value;
  }
}

function onPointerMove(e) {
  if (!punterosActivos.has(e.pointerId)) return;

  const anterior = punterosActivos.get(e.pointerId);
  const actual = { x: e.clientX, y: e.clientY };
  punterosActivos.set(e.pointerId, actual);

  if (punterosActivos.size === 1) {
    // Arrastre simple (Pan)
    const dx = actual.x - anterior.x;
    const dy = actual.y - anterior.y;

    if (Math.abs(dx) > 1 || Math.abs(dy) > 1) {
      direccion_elegida.value = false;
      direccionActual.value = '';
    }

    const z = zoom.value;
    const centroPix = a_pixel(centroLat.value, centroLng.value, z);
    const nuevoPixX = centroPix.x - dx;
    const nuevoPixY = centroPix.y - dy;

    const nuevasCoords = a_grados(nuevoPixX, nuevoPixY, z);
    centroLat.value = Math.max(-85, Math.min(85, nuevasCoords.lat));
    centroLng.value = ((nuevasCoords.lng + 180) % 360 + 360) % 360 - 180;
  } else if (punterosActivos.size === 2) {
    // Gesto de Pellizco (Pinch to Zoom)
    const pts = Array.from(punterosActivos.values());
    const distActual = Math.hypot(pts[0].x - pts[1].x, pts[0].y - pts[1].y);
    if (distanciaInicialPinch > 0) {
      const escala = distActual / distanciaInicialPinch;
      const deltaZoom = Math.round(Math.log2(escala));
      const nuevoZoom = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, zoomInicialPinch + deltaZoom));
      if (nuevoZoom !== zoom.value) {
        zoom.value = nuevoZoom;
      }
    }
  }
}

function onPointerUp(e) {
  punterosActivos.delete(e.pointerId);
  if (punterosActivos.size < 2) {
    distanciaInicialPinch = 0;
  }
  if (punterosActivos.size === 0) {
    programarGeocodificacionInversa();
  }
}

function onWheel(e) {
  if (e.deltaY < 0) {
    ajustarZoom(1);
  } else if (e.deltaY > 0) {
    ajustarZoom(-1);
  }
}

function ajustarZoom(delta) {
  const nuevo = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, zoom.value + delta));
  if (nuevo !== zoom.value) {
    zoom.value = nuevo;
    direccion_elegida.value = false;
    direccionActual.value = '';
    programarGeocodificacionInversa();
  }
}

// ----------------------------------------------------
// Geocodificación Inversa (Reverse Geocode)
// ----------------------------------------------------
function programarGeocodificacionInversa() {
  clearTimeout(timerGeocodificacion);
  cargandoDireccionInversa.value = true;
  timerGeocodificacion = setTimeout(async () => {
    try {
      const res = await buscar_direccion_de_punto(centroLat.value, centroLng.value);
      if (res && !direccion_elegida.value) {
        direccionActual.value = res;
      }
    } finally {
      cargandoDireccionInversa.value = false;
    }
  }, 400);
}

// ----------------------------------------------------
// Búsqueda de Direcciones
// ----------------------------------------------------
function onInputBusqueda() {
  if (!textoBusqueda.value || !textoBusqueda.value.trim()) {
    sugerencias.value = [];
  }
}

async function ejecutarBusqueda() {
  const query = textoBusqueda.value ? textoBusqueda.value.trim() : '';
  if (!query) return;

  buscandoDireccion.value = true;
  try {
    const res = await buscar_direcciones(query);
    sugerencias.value = res;
    if (res.length === 0) {
      vibracion_service.vibrar_error();
    } else {
      vibracion_service.vibrar_toque();
    }
  } catch (err) {
    console.warn('Error al buscar dirección:', err);
    sugerencias.value = [];
  } finally {
    buscandoDireccion.value = false;
  }
}

function seleccionarSugerencia(sug) {
  centroLat.value = sug.latitud;
  centroLng.value = sug.longitud;
  direccionActual.value = sug.direccion;
  direccion_elegida.value = true;
  zoom.value = 17;
  sugerencias.value = [];
  textoBusqueda.value = sug.direccion;
  vibracion_service.vibrar_toque();
}

// ----------------------------------------------------
// Botón GPS
// ----------------------------------------------------
async function centrarEnGpsActual() {
  obteniendoGps.value = true;
  await vibracion_service.vibrar_toque();
  try {
    const res = await obtener_ubicacion_actual();
    if (res.ok && res.ubicacion) {
      centroLat.value = res.ubicacion.latitud;
      centroLng.value = res.ubicacion.longitud;
      zoom.value = 17;
      direccion_elegida.value = false;
      direccionActual.value = '';
      programarGeocodificacionInversa();
    }
  } catch (e) {
    console.warn('No se pudo centrar en GPS:', e);
  } finally {
    obteniendoGps.value = false;
  }
}

// ----------------------------------------------------
// Ciclo de vida y dimensiones
// ----------------------------------------------------
function medirContenedor() {
  if (contenedorMapaRef.value) {
    const rect = contenedorMapaRef.value.getBoundingClientRect();
    if (rect.width > 0 && rect.height > 0) {
      anchoMapa.value = rect.width;
      altoMapa.value = rect.height;
    }
  }
}

function alAbrirModal() {
  tiles_cargadas.value = 0;
  tiles_con_error.value = 0;
  mapa_caido.value = false;
  direccion_elegida.value = Boolean(props.direccion);

  const latProp = parseFloat(props.latitud);
  const lngProp = parseFloat(props.longitud);

  if (!isNaN(latProp) && !isNaN(lngProp) && Math.abs(latProp) <= 90 && Math.abs(lngProp) <= 180) {
    centroLat.value = latProp;
    centroLng.value = lngProp;
  } else {
    centroLat.value = DEFAULT_LAT;
    centroLng.value = DEFAULT_LNG;
    direccion_elegida.value = false;
    // Si no tiene coordenadas previas, intentar ubicar GPS automáticamente
    centrarEnGpsActual();
  }

  direccionActual.value = props.direccion || '';
  textoBusqueda.value = props.direccion || '';
  sugerencias.value = [];

  nextTick(() => {
    medirContenedor();
    if (!direccionActual.value) {
      programarGeocodificacionInversa();
    }
  });
}

function cerrarModal() {
  clearTimeout(timerGeocodificacion);
  sugerencias.value = [];
  emit('cerrar');
}

async function confirmarPunto() {
  clearTimeout(timerGeocodificacion);
  await vibracion_service.vibrar_toque();
  const lat = Number(centroLat.value.toFixed(6));
  const lng = Number(centroLng.value.toFixed(6));

  let dir = (direccionActual.value || '').trim();

  // Si no hay direccion_elegida previa o no hay dirección resuelta, ejecutar y esperar la respuesta
  if (!direccion_elegida.value || !dir) {
    try {
      cargandoDireccionInversa.value = true;
      const res = await buscar_direccion_de_punto(lat, lng);
      if (res && res.trim()) {
        dir = res.trim();
        direccionActual.value = dir;
        direccion_elegida.value = true;
      }
    } catch (err) {
      console.warn('Error resolviendo dirección al confirmar:', err);
    } finally {
      cargandoDireccionInversa.value = false;
    }
  }

  emit('confirmar', {
    latitud: lat,
    longitud: lng,
    direccion: dir || textoBusqueda.value || '',
  });
  cerrarModal();
}

onMounted(() => {
  if (typeof ResizeObserver !== 'undefined' && contenedorMapaRef.value) {
    resizeObserver = new ResizeObserver(() => medirContenedor());
    resizeObserver.observe(contenedorMapaRef.value);
  }
});

onUnmounted(() => {
  clearTimeout(timerGeocodificacion);
  if (resizeObserver) {
    resizeObserver.disconnect();
  }
});
</script>

<style scoped>
.modal-selector-mapa {
  --width: 100%;
  --height: 100%;
}

.toolbar-buscador {
  --background: var(--ion-color-step-50, #f8fafc);
  padding: 4px 8px;
  position: relative;
}

.buscador-mapa-fila {
  display: flex;
  align-items: center;
  gap: 8px;
}

.input-buscador-mapa {
  padding: 0;
  --box-shadow: none;
  --background: #ffffff;
  border-radius: 12px;
}

.boton-buscar-mapa {
  height: 42px;
  margin: 0;
  border-radius: 12px;
}

.lista-sugerencias-mapa {
  position: absolute;
  top: 100%;
  left: 8px;
  right: 8px;
  background: #ffffff;
  border-radius: 12px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
  max-height: 220px;
  overflow-y: auto;
  z-index: 1000;
  border: 1px solid var(--ion-color-step-200, #e2e8f0);
}

.item-sugerencia {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 14px;
  border-bottom: 1px solid var(--ion-color-step-100, #f1f5f9);
  cursor: pointer;
  transition: background 0.15s ease;
}

.item-sugerencia:active,
.item-sugerencia:hover {
  background: var(--ion-color-step-50, #f8fafc);
}

.icono-sug {
  font-size: 1.3rem;
  color: var(--ion-color-primary, #3880ff);
  flex-shrink: 0;
}

.texto-sug {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.texto-sug strong {
  font-size: 0.88rem;
  color: var(--ion-color-step-900, #0f172a);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.texto-sug small {
  font-size: 0.75rem;
  color: var(--ion-color-step-600, #64748b);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.contenido-mapa {
  position: relative;
  width: 100%;
  height: 100%;
  --overflow: hidden;
}

.contenedor-mapa {
  position: relative;
  width: 100%;
  height: calc(100% - 130px);
  background: #e5e3df;
  overflow: hidden;
  touch-action: none;
  user-select: none;
  cursor: grab;
}

.contenedor-mapa:active {
  cursor: grabbing;
}

.capa-tiles {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
}

.tile-imagen {
  position: absolute;
  pointer-events: none;
  image-rendering: auto;
}

/* Pin Marcador Fijo en el centro */
.pin-marcador-centro {
  position: absolute;
  left: 50%;
  top: 50%;
  transform: translate(-50%, -100%);
  pointer-events: none;
  z-index: 50;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.pin-icono-envoltorio {
  animation: reboteSuave 1.5s ease-in-out infinite alternate;
}

.pin-icono {
  font-size: 44px;
  color: #ef4444;
  filter: drop-shadow(0 4px 6px rgba(0, 0, 0, 0.35));
}

.pin-sombra {
  width: 14px;
  height: 6px;
  background: rgba(0, 0, 0, 0.35);
  border-radius: 50%;
  margin-top: -6px;
  filter: blur(1px);
}

@keyframes reboteSuave {
  from {
    transform: translateY(0);
  }
  to {
    transform: translateY(-4px);
  }
}

/* Atribución de OpenStreetMap */
.atribucion-osm {
  position: absolute;
  bottom: 6px;
  left: 6px;
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(4px);
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 0.68rem;
  color: #334155;
  z-index: 40;
}

.atribucion-osm a {
  color: var(--ion-color-primary, #3880ff);
  text-decoration: none;
}

/* Controles de Zoom y Mi Ubicación */
.controles-mapa-flotantes {
  position: absolute;
  right: 12px;
  bottom: 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  z-index: 40;
}

.boton-ctrl-mapa {
  width: 44px;
  height: 44px;
  margin: 0;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
}

/* Aviso si el mapa se cae */
.aviso-caida-flotante {
  position: absolute;
  top: 10px;
  left: 10px;
  right: 10px;
  z-index: 60;
}

/* Barra inferior de confirmación */
.barra-confirmacion-mapa {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 130px;
  background: #ffffff;
  border-top: 1px solid var(--ion-color-step-200, #e2e8f0);
  padding: 12px 16px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  box-shadow: 0 -4px 16px rgba(0, 0, 0, 0.08);
  z-index: 50;
}

.info-punto-actual {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.direccion-resumida {
  display: flex;
  align-items: center;
  gap: 6px;
}

.texto-direccion {
  font-size: 0.95rem;
  font-weight: 700;
  color: var(--ion-color-step-900, #0f172a);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.texto-cargando {
  font-size: 0.85rem;
  color: var(--ion-color-primary, #3880ff);
  font-style: italic;
}

.coordenadas-detalle {
  color: var(--ion-color-step-600, #64748b);
  padding-left: 22px;
}

.boton-confirmar-ubicacion {
  margin: 0;
  font-weight: 700;
  letter-spacing: 0.3px;
}

.spinner-sm {
  width: 18px;
  height: 18px;
}
</style>
