<template>
  <ion-modal :is-open="is_open" @didDismiss="cerrar">
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>{{ cliente?.id ? 'Editar Cliente' : 'Nuevo Cliente' }}</ion-title>
        <ion-buttons slot="end">
          <ion-button @click="cerrar">Cerrar</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <div v-if="error_mensaje" class="ion-margin-bottom">
        <ion-note color="danger" class="error-note">
          <ion-icon :icon="alertCircleOutline" class="icono-error"></ion-icon>
          <span>{{ error_mensaje }}</span>
        </ion-note>
      </div>

      <!-- Sección de Foto / Avatar del Cliente -->
      <div class="seccion-foto ion-text-center ion-margin-bottom">
        <div class="contenedor-avatar">
          <img
            v-if="urlFotoActual"
            :src="urlFotoActual"
            alt="Foto del cliente"
            class="imagen-avatar"
          />
          <ion-icon
            v-else
            :icon="personCircleOutline"
            class="icono-avatar-defecto"
          ></ion-icon>
        </div>

        <!-- Acciones para captura / selección de imagen -->
        <div class="botones-foto ion-margin-top">
          <template v-if="esNativo">
            <ion-button size="small" fill="outline" shape="round" @click="capturarFoto('camara')">
              <ion-icon slot="start" :icon="cameraOutline"></ion-icon>
              Cámara
            </ion-button>
            <ion-button size="small" fill="outline" shape="round" color="secondary" @click="capturarFoto('galeria')">
              <ion-icon slot="start" :icon="imagesOutline"></ion-icon>
              Galería
            </ion-button>
          </template>

          <template v-else>
            <input
              type="file"
              ref="inputArchivoRef"
              accept="image/png,image/jpeg,image/webp,image/jpg"
              style="display: none"
              @change="onArchivoWebSeleccionado"
            />
            <ion-button size="small" fill="outline" shape="round" @click="abrirSelectorArchivoWeb">
              <ion-icon slot="start" :icon="cameraOutline"></ion-icon>
              Seleccionar Foto
            </ion-button>
          </template>

          <ion-button
            v-if="urlFotoActual"
            size="small"
            fill="clear"
            color="danger"
            shape="round"
            @click="quitarFoto"
          >
            <ion-icon slot="start" :icon="trashOutline"></ion-icon>
            Quitar foto
          </ion-button>
        </div>
      </div>

      <!-- Formulario de campos de cliente -->
      <ion-item>
        <ion-label position="stacked">Nombre completo *</ion-label>
        <ion-input v-model="form.nombre" type="text" placeholder="Ej: Juan Pérez" required></ion-input>
      </ion-item>
      <ion-item>
        <ion-label position="stacked">Correo electrónico</ion-label>
        <ion-input v-model="form.email" type="email" placeholder="ejemplo@correo.com"></ion-input>
      </ion-item>
      <ion-item>
        <ion-label position="stacked">Teléfono de contacto *</ion-label>
        <ion-input v-model="form.telefono" type="tel" placeholder="Ej: 2664123456" required></ion-input>
      </ion-item>
      <ion-item>
        <ion-label position="stacked">Dirección</ion-label>
        <ion-input v-model="form.direccion" type="text" placeholder="Calle y número"></ion-input>
      </ion-item>
      
      <ion-button expand="block" class="ion-margin-top" shape="round" @click="guardar" :disabled="guardando">
        {{ guardando ? 'Guardando...' : (cliente?.id ? 'Actualizar Cliente' : 'Crear Cliente') }}
      </ion-button>
    </ion-content>
  </ion-modal>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import {
  IonModal,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonButton,
  IonContent,
  IonItem,
  IonLabel,
  IonInput,
  IonNote,
  IonIcon,
} from '@ionic/vue';
import {
  cameraOutline,
  imagesOutline,
  trashOutline,
  personCircleOutline,
  alertCircleOutline,
} from 'ionicons/icons';
import { crear_cliente, actualizar_cliente } from '@/services/clientes_service';
import { camara_service } from '@/services/camara_service';
import { vibrar_toque, vibrar_error } from '@/services/vibracion_service';
import { obtener_api_url } from '@/config/debug';

const props = defineProps({
  is_open: { type: Boolean, required: true },
  cliente: { type: Object, default: () => null },
});

const emit = defineEmits(['cerrar', 'guardado']);

const form = ref({
  nombre: '',
  email: '',
  telefono: '',
  direccion: '',
});

const fotoArchivo = ref(null);
const previewLocal = ref(null);
const fotoEliminada = ref(false);
const guardando = ref(false);
const error_mensaje = ref(null);
const inputArchivoRef = ref(null);

const esNativo = camara_service.camara_disponible();

const urlFotoActual = computed(() => {
  if (previewLocal.value) {
    return previewLocal.value;
  }
  if (!fotoEliminada.value && props.cliente?.foto_url) {
    const url = props.cliente.foto_url;
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    return `${obtener_api_url()}${url.startsWith('/') ? '' : '/'}${url}`;
  }
  return null;
});

watch(
  () => props.cliente,
  (nuevo_cliente) => {
    error_mensaje.value = null;
    fotoArchivo.value = null;
    previewLocal.value = null;
    fotoEliminada.value = false;

    if (nuevo_cliente && nuevo_cliente.id) {
      form.value = {
        id: nuevo_cliente.id,
        nombre: nuevo_cliente.nombre || '',
        email: nuevo_cliente.email || '',
        telefono: nuevo_cliente.telefono || '',
        direccion: nuevo_cliente.direccion || '',
      };
    } else {
      form.value = {
        nombre: '',
        email: '',
        telefono: '',
        direccion: '',
      };
    }
  },
  { immediate: true },
);

watch(
  () => props.is_open,
  (abierto) => {
    if (abierto) {
      error_mensaje.value = null;
    }
  },
);

async function capturarFoto(origen) {
  error_mensaje.value = null;
  const res = await camara_service.tomar_foto(origen);
  if (res.ok && res.archivo) {
    await vibrar_toque();
    fotoArchivo.value = res.archivo;
    fotoEliminada.value = false;
    previewLocal.value = URL.createObjectURL(res.archivo);
  } else if (res.mensaje) {
    await vibrar_error();
    error_mensaje.value = res.mensaje;
  }
}

function abrirSelectorArchivoWeb() {
  if (inputArchivoRef.value) {
    inputArchivoRef.value.click();
  }
}

async function onArchivoWebSeleccionado(evento) {
  const archivo = evento.target.files?.[0];
  if (archivo) {
    await vibrar_toque();
    fotoArchivo.value = archivo;
    fotoEliminada.value = false;
    previewLocal.value = URL.createObjectURL(archivo);
  }
}

async function quitarFoto() {
  await vibrar_toque();
  fotoArchivo.value = null;
  previewLocal.value = null;
  fotoEliminada.value = true;
  if (inputArchivoRef.value) {
    inputArchivoRef.value.value = '';
  }
}

const cerrar = () => {
  error_mensaje.value = null;
  emit('cerrar');
};

const guardar = async () => {
  error_mensaje.value = null;
  guardando.value = true;
  try {
    await vibrar_toque();
    if (props.cliente?.id) {
      await actualizar_cliente(props.cliente.id, form.value, fotoArchivo.value);
    } else {
      await crear_cliente(form.value, fotoArchivo.value);
    }
    emit('guardado', {
      id: props.cliente?.id,
      datos: form.value,
      foto: fotoArchivo.value,
    });
    cerrar();
  } catch (error) {
    await vibrar_error();
    error_mensaje.value = error.mensaje || error.message || 'Error al procesar la solicitud.';
  } finally {
    guardando.value = false;
  }
};
</script>

<style scoped>
.seccion-foto {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-top: 0.5rem;
}

.contenedor-avatar {
  width: 104px;
  height: 104px;
  border-radius: 50%;
  overflow: hidden;
  border: 3px solid var(--ion-color-primary, #3880ff);
  background: var(--ion-color-step-100, #f2f2f2);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.12);
}

.imagen-avatar {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.icono-avatar-defecto {
  font-size: 110px;
  color: var(--ion-color-medium, #92949c);
}

.botones-foto {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  justify-content: center;
}

.error-note {
  font-size: 0.95rem;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  background: rgba(var(--ion-color-danger-rgb, 235, 68, 90), 0.1);
  border-radius: 8px;
}

.icono-error {
  font-size: 20px;
}
</style>
