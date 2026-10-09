<template>
  <ion-modal :is-open="is_open" @didDismiss="cerrar">
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>Nuevo Pedido</ion-title>
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

      <!-- Selección de Cliente -->
      <ion-item>
        <ion-label position="stacked">Cliente *</ion-label>
        <ion-select
          v-model="form.cliente_id"
          placeholder="Seleccionar cliente..."
          interface="action-sheet"
          @ionChange="alCambiarCliente"
        >
          <ion-select-option
            v-for="cli in clientes"
            :key="cli.id"
            :value="cli.id"
          >
            {{ cli.nombre }} ({{ cli.telefono }})
          </ion-select-option>
        </ion-select>
      </ion-item>

      <!-- Advertencia si el cliente no tiene dirección y se elige entrega -->
      <div v-if="clienteSeleccionado" class="info-cliente ion-margin-top ion-padding">
        <p class="detalle-cliente">
          <strong>Dirección:</strong> {{ clienteSeleccionado.direccion || 'Sin dirección registrada' }}
        </p>
        <p v-if="clienteSeleccionado.direccion_latitud && clienteSeleccionado.direccion_longitud" class="detalle-gps">
          <ion-icon :icon="locationOutline"></ion-icon>
          GPS: {{ formatear_coordenadas(clienteSeleccionado.direccion_latitud, clienteSeleccionado.direccion_longitud) }}
        </p>
      </div>

      <!-- Tipo de Pedido: Mostrador o Entrega -->
      <div class="seccion-tipo ion-margin-top">
        <ion-label class="etiqueta-seccion">Tipo de Entrega</ion-label>
        <ion-segment v-model="form.tipo" class="ion-margin-top">
          <ion-segment-button value="mostrador">
            <ion-icon :icon="storefrontOutline"></ion-icon>
            <ion-label>Mostrador</ion-label>
          </ion-segment-button>
          <ion-segment-button value="entrega">
            <ion-icon :icon="bicycleOutline"></ion-icon>
            <ion-label>A Domicilio</ion-label>
          </ion-segment-button>
        </ion-segment>
      </div>

      <div v-if="form.tipo === 'entrega' && (!clienteSeleccionado || !clienteSeleccionado.direccion)" class="aviso-alerta ion-margin-top">
        <ion-icon :icon="alertCircleOutline" color="warning"></ion-icon>
        <span>El cliente seleccionado debe tener una dirección para solicitar reparto.</span>
      </div>

      <!-- Selección de Productos / Ítems -->
      <div class="seccion-items ion-margin-top">
        <div class="cabecera-items">
          <ion-label class="etiqueta-seccion">Productos del Pedido</ion-label>
          <ion-button size="small" fill="outline" shape="round" @click="agregarItem">
            <ion-icon slot="start" :icon="addOutline"></ion-icon>
            Agregar producto
          </ion-button>
        </div>

        <div v-if="form.items.length === 0" class="sin-items ion-text-center ion-padding">
          <p>Presione "Agregar producto" para incluir artículos en el pedido.</p>
        </div>

        <div
          v-for="(item, index) in form.items"
          :key="index"
          class="fila-producto-card ion-margin-top"
        >
          <div class="fila-producto-select">
            <ion-select
              v-model="item.producto_id"
              placeholder="Producto..."
              interface="action-sheet"
              @ionChange="alSeleccionarProducto(item)"
            >
              <ion-select-option
                v-for="prod in productos"
                :key="prod.id"
                :value="prod.id"
              >
                {{ prod.nombre }} (${{ Number(prod.precio).toFixed(2) }})
              </ion-select-option>
            </ion-select>

            <ion-button
              fill="clear"
              color="danger"
              size="small"
              @click="removerItem(index)"
            >
              <ion-icon slot="icon-only" :icon="trashOutline"></ion-icon>
            </ion-button>
          </div>

          <div class="fila-cant-subtotal ion-padding-top">
            <div class="control-cantidad">
              <span class="lbl-cant">Cant:</span>
              <ion-input
                v-model.number="item.cantidad"
                type="number"
                min="1"
                class="input-cantidad"
                @ionInput="recalcularTotal"
              ></ion-input>
            </div>
            <div class="subtotal-item">
              Subtotal: <strong>${{ Number(item.subtotal || 0).toFixed(2) }}</strong>
            </div>
          </div>
        </div>
      </div>

      <!-- Observaciones / Notas -->
      <ion-item class="ion-margin-top">
        <ion-label position="stacked">Notas / Observaciones</ion-label>
        <ion-textarea
          v-model="form.notas"
          rows="2"
          placeholder="Instrucciones adicionales para la entrega..."
        ></ion-textarea>
      </ion-item>

      <!-- Total general -->
      <div class="total-general ion-margin-top ion-padding">
        <span class="total-etiqueta">Total del Pedido:</span>
        <span class="total-monto">${{ Number(totalGeneral).toLocaleString('es-AR', { minimumFractionDigits: 2 }) }}</span>
      </div>

      <!-- Botón de Confirmación -->
      <ion-button
        expand="block"
        shape="round"
        class="ion-margin-top ion-margin-bottom"
        :disabled="guardando || form.items.length === 0 || !form.cliente_id"
        @click="guardarPedido"
      >
        <ion-spinner v-if="guardando" name="crescent" slot="start"></ion-spinner>
        {{ guardando ? 'Guardando pedido...' : 'Confirmar y Crear Pedido' }}
      </ion-button>
    </ion-content>
  </ion-modal>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
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
  IonSelect,
  IonSelectOption,
  IonSegment,
  IonSegmentButton,
  IonInput,
  IonTextarea,
  IonNote,
  IonIcon,
  IonSpinner,
} from '@ionic/vue';
import {
  addOutline,
  trashOutline,
  storefrontOutline,
  bicycleOutline,
  locationOutline,
  alertCircleOutline,
} from 'ionicons/icons';
import { enviar } from '@/services/ajax_service';
import { vibrar_toque, vibrar_error } from '@/services/vibracion_service';
import { formatear_coordenadas } from '@/services/geolocalizacion_service';

const props = defineProps({
  is_open: { type: Boolean, required: true },
});

const emit = defineEmits(['cerrar', 'creado']);

const clientes = ref([]);
const productos = ref([]);
const guardando = ref(false);
const error_mensaje = ref(null);

const form = ref({
  cliente_id: null,
  tipo: 'mostrador',
  notas: '',
  items: [],
});

const clienteSeleccionado = computed(() => {
  return clientes.value.find((c) => c.id === form.value.cliente_id);
});

const totalGeneral = computed(() => {
  return form.value.items.reduce((acc, curr) => acc + (Number(curr.subtotal) || 0), 0);
});

async function cargarCatalogos() {
  try {
    const [resCli, resProd] = await Promise.all([
      enviar('/api/clientes?tamano=100'),
      enviar('/api/productos/resumen'),
    ]);
    clientes.value = resCli.clientes ?? [];
    productos.value = resProd.productos ?? [];
  } catch (err) {
    console.error('Error al cargar catálogos para pedido:', err);
  }
}

function alCambiarCliente() {
  vibrar_toque();
}

function agregarItem() {
  vibrar_toque();
  const prodDefecto = productos.value[0];
  form.value.items.push({
    producto_id: prodDefecto ? prodDefecto.id : null,
    cantidad: 1,
    precio_unitario: prodDefecto ? prodDefecto.precio : 0,
    subtotal: prodDefecto ? prodDefecto.precio : 0,
  });
}

function removerItem(index) {
  vibrar_toque();
  form.value.items.splice(index, 1);
}

function alSeleccionarProducto(item) {
  const prod = productos.value.find((p) => p.id === item.producto_id);
  if (prod) {
    item.precio_unitario = prod.precio;
    item.subtotal = (item.cantidad || 1) * prod.precio;
  }
}

function recalcularTotal() {
  for (const item of form.value.items) {
    const prod = productos.value.find((p) => p.id === item.producto_id);
    const precio = prod ? prod.precio : item.precio_unitario || 0;
    item.subtotal = (Number(item.cantidad) || 0) * precio;
  }
}

function cerrar() {
  error_mensaje.value = null;
  emit('cerrar');
}

async function guardarPedido() {
  if (guardando.value) return;
  error_mensaje.value = null;

  if (!form.value.cliente_id) {
    error_mensaje.value = 'Debe seleccionar un cliente.';
    return;
  }

  if (form.value.tipo === 'entrega' && (!clienteSeleccionado.value || !clienteSeleccionado.value.direccion)) {
    error_mensaje.value = 'El cliente debe poseer una dirección registrada para pedidos con entrega a domicilio.';
    return;
  }

  const itemsValidos = form.value.items.filter((i) => i.producto_id && i.cantidad > 0);
  if (itemsValidos.length === 0) {
    error_mensaje.value = 'Debe incluir al menos un producto en el pedido.';
    return;
  }

  guardando.value = true;
  try {
    await vibrar_toque();
    const payload = {
      cliente_id: form.value.cliente_id,
      tipo: form.value.tipo,
      notas: form.value.notas,
      items: itemsValidos.map((i) => ({
        producto_id: i.producto_id,
        cantidad: Number(i.cantidad),
        precio_unitario: Number(i.precio_unitario),
      })),
    };

    const creado = await enviar('/api/pedidos', 'POST', payload);

    emit('creado', creado);
    cerrar();
  } catch (err) {
    await vibrar_error();
    error_mensaje.value = err.mensaje || err.message || 'Error al crear el pedido.';
  } finally {
    guardando.value = false;
  }
}

watch(
  () => props.is_open,
  (abierto) => {
    if (abierto) {
      error_mensaje.value = null;
      form.value = {
        cliente_id: null,
        tipo: 'mostrador',
        notas: '',
        items: [],
      };
      cargarCatalogos();
    }
  }
);

onMounted(() => {
  if (props.is_open) {
    cargarCatalogos();
  }
});
</script>

<style scoped>
.error-note {
  font-size: 0.92rem;
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

.etiqueta-seccion {
  font-weight: 600;
  font-size: 0.95rem;
  color: var(--ion-color-step-800, #1e293b);
}

.info-cliente {
  background: var(--ion-color-step-50, #f8fafc);
  border: 1px solid var(--ion-color-step-150, #e2e8f0);
  border-radius: 10px;
}

.detalle-cliente {
  font-size: 0.88rem;
  margin: 0;
  color: var(--ion-color-step-700, #334155);
}

.detalle-gps {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.82rem;
  font-family: monospace;
  color: var(--ion-color-primary, #3880ff);
  margin: 4px 0 0;
}

.aviso-alerta {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.85rem;
  color: var(--ion-color-warning-shade, #b45309);
  background: rgba(var(--ion-color-warning-rgb, 255, 196, 9), 0.15);
  padding: 8px 12px;
  border-radius: 8px;
}

.cabecera-items {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.sin-items {
  font-size: 0.85rem;
  font-style: italic;
  color: var(--ion-color-medium, #94a3b8);
}

.fila-producto-card {
  background: var(--ion-color-step-50, #f8fafc);
  border: 1px solid var(--ion-color-step-150, #e2e8f0);
  border-radius: 10px;
  padding: 10px;
}

.fila-producto-select {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.fila-cant-subtotal {
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-top: 1px dashed var(--ion-color-step-150, #e2e8f0);
  margin-top: 6px;
}

.control-cantidad {
  display: flex;
  align-items: center;
  gap: 6px;
}

.lbl-cant {
  font-size: 0.85rem;
  font-weight: 500;
}

.input-cantidad {
  width: 70px;
  --padding-start: 6px;
  --padding-end: 6px;
  border: 1px solid var(--ion-color-step-200, #cbd5e1);
  border-radius: 6px;
  text-align: center;
}

.subtotal-item {
  font-size: 0.9rem;
  color: var(--ion-color-step-800, #1e293b);
}

.total-general {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: rgba(var(--ion-color-success-rgb, 45, 211, 111), 0.08);
  border: 1px solid rgba(var(--ion-color-success-rgb, 45, 211, 111), 0.25);
  border-radius: 12px;
}

.total-etiqueta {
  font-size: 1.05rem;
  font-weight: 700;
}

.total-monto {
  font-size: 1.35rem;
  font-weight: 800;
  color: var(--ion-color-success, #10b981);
}
</style>
