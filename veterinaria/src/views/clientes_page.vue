<template>
  <comp-page titulo="Clientes" :mostrar_actualizar="true" @actualizar="cargar">
    <comp-buscador placeholder="Buscar clientes..." @buscar="onBuscar" />
    
    <comp-esqueleto v-if="clientes_store.cargando && clientes_store.clientes.length === 0" />
    
    <div v-else-if="clientes_store.clientes.length === 0" class="ion-padding ion-text-center">
      <p>No hay clientes registrados.</p>
    </div>

    <comp-lista
      v-else
      :total="clientes_store.paginacion.total"
      :cantidad_mostrada="clientes_store.clientes.length"
      :hay_mas="clientes_store.paginacion.hay_mas"
      :cargando="clientes_store.cargando"
      @ver_mas="cargarMas"
    >
      <template #lista>
        <ion-list>
          <ion-item-sliding v-for="c in clientes_store.clientes" :key="c.id">
            <ion-item button @click="abrirModal(c)">
              <ion-avatar slot="start" class="avatar-cliente">
                <img v-if="c.foto_url" :src="resolverFoto(c.foto_url)" alt="Foto cliente" />
                <ion-icon v-else :icon="personCircleOutline" class="icono-avatar-lista"></ion-icon>
              </ion-avatar>
              <ion-label>
                <h2>{{ c.nombre }}</h2>
                <p>Email: {{ c.email }} | Tel: {{ c.telefono }}</p>
                <p v-if="c.direccion">{{ c.direccion }}</p>
                <p v-else style="color: var(--ion-color-medium); font-style: italic">
                  Sin dirección
                </p>
              </ion-label>
              <ion-button slot="end" fill="clear" color="danger" @click.stop="confirmarBaja(c)">
                <ion-icon slot="icon-only" :icon="trashOutline"></ion-icon>
              </ion-button>
            </ion-item>

            <ion-item-options side="end">
              <ion-item-option color="danger" @click="confirmarBaja(c)">
                <ion-icon slot="icon-only" :icon="trashOutline"></ion-icon>
              </ion-item-option>
            </ion-item-options>
          </ion-item-sliding>
        </ion-list>
      </template>
    </comp-lista>
    
    <ion-fab vertical="bottom" horizontal="end" slot="fixed">
      <ion-fab-button @click="abrirModal(null)">
        <ion-icon :icon="add"></ion-icon>
      </ion-fab-button>
    </ion-fab>

    <modal-cliente
      :is_open="modalAbierto"
      :cliente="clienteSeleccionado"
      @cerrar="modalAbierto = false"
      @guardado="onClienteGuardado"
    />
  </comp-page>
</template>

<script setup>
import { ref, onMounted } from "vue";
import {
  IonList,
  IonItem,
  IonItemSliding,
  IonItemOptions,
  IonItemOption,
  IonLabel,
  IonButton,
  IonFab,
  IonFabButton,
  IonIcon,
  IonAvatar,
  alertController,
} from "@ionic/vue";
import { add, trashOutline, personCircleOutline } from 'ionicons/icons';
import compPage from "@/components/estructura/comp_page.vue";
import compEsqueleto from "@/components/base/comp_esqueleto.vue";
import compBuscador from "@/components/base/comp_buscador.vue";
import compLista from "@/components/base/comp_lista.vue";
import modalCliente from "@/components/dominio/modal_cliente.vue";
import { clientes_store } from "@/stores/clientes_store";
import { eliminar_cliente } from "@/services/clientes_service";
import { vibrar_toque, vibrar_error } from "@/services/vibracion_service";
import { obtener_api_url } from "@/config/debug";

const modalAbierto = ref(false);
const clienteSeleccionado = ref(null);

function resolverFoto(fotoUrl) {
  if (!fotoUrl) return '';
  if (fotoUrl.startsWith('http://') || fotoUrl.startsWith('https://')) return fotoUrl;
  return `${obtener_api_url()}${fotoUrl.startsWith('/') ? '' : '/'}${fotoUrl}`;
}

async function cargar(event = null) {
  clientes_store.reiniciar_paginacion();
  await clientes_store.cargar_clientes();
  if (event?.target?.complete) {
    event.target.complete();
  }
}

async function cargarMas() {
  await clientes_store.cargar_mas_clientes();
}

async function onBuscar(termino) {
  await clientes_store.buscar(termino);
}

async function abrirModal(cliente) {
  await vibrar_toque();
  clienteSeleccionado.value = cliente;
  modalAbierto.value = true;
}

async function onClienteGuardado() {
  modalAbierto.value = false;
  await cargar();
}

async function confirmarBaja(cliente) {
  await vibrar_toque();
  const alert = await alertController.create({
    header: 'Dar de baja',
    message: `¿Desea dar de baja al cliente "${cliente.nombre}"?`,
    buttons: [
      {
        text: 'Cancelar',
        role: 'cancel',
      },
      {
        text: 'Dar de baja',
        role: 'destructive',
        handler: async () => {
          await ejecutarBaja(cliente.id);
        },
      },
    ],
  });
  await alert.present();
}

async function ejecutarBaja(id) {
  try {
    await vibrar_toque();
    await eliminar_cliente(id);
    await cargar();
  } catch (error) {
    await vibrar_error();
    console.error('Error al dar de baja:', error);
  }
}

onMounted(() => cargar());
</script>

<style scoped>
.avatar-cliente {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--ion-color-step-100, #f2f2f2);
  border-radius: 50%;
  overflow: hidden;
}

.avatar-cliente img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.icono-avatar-lista {
  font-size: 52px;
  color: var(--ion-color-medium, #92949c);
}
</style>

