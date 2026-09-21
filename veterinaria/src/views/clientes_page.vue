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
  alertController,
} from "@ionic/vue";
import { add, trashOutline } from 'ionicons/icons';
import compPage from "@/components/estructura/comp_page.vue";
import compEsqueleto from "@/components/base/comp_esqueleto.vue";
import compBuscador from "@/components/base/comp_buscador.vue";
import compLista from "@/components/base/comp_lista.vue";
import modalCliente from "@/components/dominio/modal_cliente.vue";
import { clientes_store } from "@/stores/clientes_store";
import { eliminar_cliente } from "@/services/clientes_service";

const modalAbierto = ref(false);
const clienteSeleccionado = ref(null);

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

function abrirModal(cliente) {
  clienteSeleccionado.value = cliente;
  modalAbierto.value = true;
}

async function onClienteGuardado() {
  modalAbierto.value = false;
  await cargar();
}

async function confirmarBaja(cliente) {
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
    await eliminar_cliente(id);
    await cargar();
  } catch (error) {
    console.error('Error al dar de baja:', error);
  }
}

onMounted(() => cargar());
</script>

