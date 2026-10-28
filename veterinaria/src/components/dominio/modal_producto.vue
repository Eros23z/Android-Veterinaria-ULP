<template>
  <ion-modal :is-open="is_open" class="vet-modal-flotante" @didDismiss="cerrar">
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>{{ producto?.id ? 'Editar Producto' : 'Nuevo Producto' }}</ion-title>
        <ion-buttons slot="end">
          <ion-button @click="cerrar">
            <ion-icon slot="icon-only" :icon="closeOutline"></ion-icon>
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <ion-item>
        <ion-label position="stacked">Nombre del producto *</ion-label>
        <ion-input v-model="form.nombre" placeholder="Ej: Vacuna Séxtuple Canina" required></ion-input>
      </ion-item>

      <ion-item>
        <ion-label position="stacked">Precio ($) *</ion-label>
        <ion-input v-model.number="form.precio" type="number" min="0" step="0.01" placeholder="0.00" required></ion-input>
      </ion-item>

      <ion-item class="ion-margin-top">
        <ion-label position="stacked">Descripción del producto</ion-label>
        <div class="fila-input-dictado">
          <ion-textarea
            v-model="form.descripcion"
            rows="3"
            placeholder="Indicaciones, posología, detalles..."
          ></ion-textarea>
          <comp-dictado-boton :valor="form.descripcion" @actualizar="form.descripcion = $event" />
        </div>
      </ion-item>

      <ion-item class="ion-margin-top">
        <ion-checkbox v-model="form.disponible">Disponible en stock</ion-checkbox>
      </ion-item>

      <div class="ion-margin-top ion-margin-bottom">
        <ion-button
          expand="block"
          shape="round"
          :disabled="!form.nombre || form.precio == null"
          @click="guardar"
        >
          {{ producto?.id ? 'Actualizar Producto' : 'Guardar Producto' }}
        </ion-button>
      </div>
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
  IonTextarea,
  IonCheckbox,
  IonIcon,
} from '@ionic/vue';
import { closeOutline } from 'ionicons/icons';
import compDictadoBoton from '@/components/base/comp_dictado_boton.vue';
import { vibracion_service } from '@/services/vibracion_service';

const props = defineProps({
  is_open: { type: Boolean, required: true },
  producto: { type: Object, default: () => null },
});

const emit = defineEmits(['cerrar', 'guardado']);

const form = ref({
  nombre: '',
  precio: 0,
  descripcion: '',
  disponible: true,
});

watch(
  () => props.producto,
  (p) => {
    if (p) {
      form.value = {
        nombre: p.nombre || '',
        precio: p.precio != null ? p.precio : 0,
        descripcion: p.descripcion || '',
        disponible: p.disponible !== false,
      };
    } else {
      form.value = {
        nombre: '',
        precio: 0,
        descripcion: '',
        disponible: true,
      };
    }
  },
  { immediate: true }
);

function cerrar() {
  emit('cerrar');
}

async function guardar() {
  await vibracion_service.vibrar_toque();
  emit('guardado', { ...form.value, id: props.producto?.id });
  cerrar();
}
</script>

<style scoped>
.fila-input-dictado {
  display: flex;
  align-items: center;
  width: 100%;
}

.fila-input-dictado ion-textarea {
  flex: 1;
}
</style>
