<template>
  <ion-modal :is-open="is_open" @didDismiss="cerrar">
    <ion-header>
      <ion-toolbar>
        <ion-title>{{ cliente?.id ? 'Editar Cliente' : 'Nuevo Cliente' }}</ion-title>
        <ion-buttons slot="end">
          <ion-button @click="cerrar">Cerrar</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content class="ion-padding">
      <div v-if="error_mensaje" class="ion-margin-bottom">
        <ion-note color="danger" class="error-note">
          {{ error_mensaje }}
        </ion-note>
      </div>

      <ion-item>
        <ion-label position="stacked">Nombre *</ion-label>
        <ion-input v-model="form.nombre" type="text" placeholder="Ingrese nombre completo"></ion-input>
      </ion-item>
      <ion-item>
        <ion-label position="stacked">Email</ion-label>
        <ion-input v-model="form.email" type="email" placeholder="ejemplo@correo.com"></ion-input>
      </ion-item>
      <ion-item>
        <ion-label position="stacked">Teléfono *</ion-label>
        <ion-input v-model="form.telefono" type="text" placeholder="Número de contacto"></ion-input>
      </ion-item>
      <ion-item>
        <ion-label position="stacked">Dirección</ion-label>
        <ion-input v-model="form.direccion" type="text" placeholder="Dirección del domicilio"></ion-input>
      </ion-item>
      
      <ion-button expand="block" class="ion-margin-top" @click="guardar" :disabled="guardando">
        {{ guardando ? 'Guardando...' : (cliente?.id ? 'Actualizar Cliente' : 'Crear Cliente') }}
      </ion-button>
    </ion-content>
  </ion-modal>
</template>

<script setup>
import { ref, watch } from 'vue';
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
} from '@ionic/vue';
import { crear_cliente, actualizar_cliente } from '@/services/clientes_service';

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

const guardando = ref(false);
const error_mensaje = ref(null);

watch(
  () => props.cliente,
  (nuevo_cliente) => {
    error_mensaje.value = null;
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

const cerrar = () => {
  error_mensaje.value = null;
  emit('cerrar');
};

const guardar = async () => {
  error_mensaje.value = null;
  guardando.value = true;
  try {
    if (props.cliente?.id) {
      await actualizar_cliente(props.cliente.id, form.value);
    } else {
      await crear_cliente(form.value);
    }
    emit('guardado');
    cerrar();
  } catch (error) {
    error_mensaje.value = error.mensaje || error.message || 'Error al procesar la solicitud.';
  } finally {
    guardando.value = false;
  }
};
</script>

<style scoped>
.error-note {
  font-size: 0.95rem;
  font-weight: 500;
  display: block;
  padding: 8px 12px;
  background: rgba(var(--ion-color-danger-rgb, 235, 68, 90), 0.1);
  border-radius: 8px;
}
</style>
