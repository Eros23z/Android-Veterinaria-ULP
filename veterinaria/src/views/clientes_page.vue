<template>
  <comp-page titulo="Clientes" :mostrar_actualizar="true" @actualizar="cargar">
    <comp-buscador placeholder="Buscar clientes..." @buscar="onBuscar" />

    <comp-esqueleto v-if="clientes_store.cargando && clientes_store.clientes.length === 0" :cantidad="4" />

    <comp-estado-vacio
      v-else-if="clientes_store.clientes.length === 0"
      :tipo="tipoVacio"
      :texto-boton="tipoVacio === 'filtros' ? 'Limpiar búsqueda' : 'Registrar nuevo cliente'"
      @accion="onAccionVacio"
    />

    <comp-lista
      v-else
      :total="clientes_store.paginacion.total"
      :cantidad_mostrada="clientes_store.clientes.length"
      :hay_mas="clientes_store.paginacion.hay_mas"
      :cargando="clientes_store.cargando"
      @ver_mas="cargarMas"
    >
      <template #lista>
        <div class="lista-tarjetas-clientes ion-padding-horizontal">
          <div
            v-for="c in clientes_store.clientes"
            :key="c.id"
            class="vet-card tarjeta-cliente"
            @click="abrirModal(c)"
          >
            <div class="tarjeta-cuerpo">
              <ion-avatar class="avatar-cliente">
                <img v-if="c.foto_url" :src="resolverFoto(c.foto_url)" alt="Foto cliente" />
                <ion-icon v-else :icon="personCircleOutline" class="icono-avatar-lista"></ion-icon>
              </ion-avatar>

              <div class="info-cliente">
                <h3 class="nombre-cliente">{{ c.nombre }}</h3>
                <p class="contacto-cliente">
                  <span>{{ c.email || 'Sin email' }}</span>
                  <span v-if="c.telefono"> · {{ c.telefono }}</span>
                </p>
                <p v-if="c.direccion" class="direccion-cliente">
                  <ion-icon :icon="locationOutline" class="icono-pin"></ion-icon>
                  <span>{{ c.direccion }}</span>
                </p>
                <p v-else class="sin-direccion">
                  Sin dirección registrada
                </p>
              </div>

              <div class="acciones-cliente">
                <ion-button
                  fill="clear"
                  color="danger"
                  class="boton-baja"
                  @click.stop="confirmarBaja(c)"
                  title="Dar de baja cliente"
                >
                  <ion-icon slot="icon-only" :icon="trashOutline"></ion-icon>
                </ion-button>
              </div>
            </div>
          </div>
        </div>
      </template>
    </comp-lista>

    <ion-fab vertical="bottom" horizontal="end" slot="fixed">
      <ion-fab-button @click="abrirModal(null)" title="Nuevo cliente">
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
import { ref, computed, onMounted } from "vue";
import {
  IonButton,
  IonFab,
  IonFabButton,
  IonIcon,
  IonAvatar,
  alertController,
} from "@ionic/vue";
import { add, trashOutline, personCircleOutline, locationOutline } from 'ionicons/icons';
import compPage from "@/components/estructura/comp_page.vue";
import compEsqueleto from "@/components/base/comp_esqueleto.vue";
import compBuscador from "@/components/base/comp_buscador.vue";
import compLista from "@/components/base/comp_lista.vue";
import compEstadoVacio from "@/components/base/comp_estado_vacio.vue";
import modalCliente from "@/components/dominio/modal_cliente.vue";
import { clientes_store } from "@/stores/clientes_store";
import { eliminar_cliente } from "@/services/clientes_service";
import { vibrar_toque, vibrar_error } from "@/services/vibracion_service";
import { obtener_api_url } from "@/config/debug";

const modalAbierto = ref(false);
const clienteSeleccionado = ref(null);
const terminoBusqueda = ref('');

const tipoVacio = computed(() => {
  return terminoBusqueda.value ? 'filtros' : 'general';
});

function onAccionVacio() {
  if (terminoBusqueda.value) {
    terminoBusqueda.value = '';
    cargar();
  } else {
    abrirModal(null);
  }
}

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
  terminoBusqueda.value = termino;
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
    header: 'Confirmar baja',
    message: `¿Dar de baja a ${cliente.nombre}? Esta acción inhabilitará al cliente en el sistema.`,
    buttons: [
      {
        text: 'Cancelar',
        role: 'cancel',
      },
      {
        text: `Dar de baja a ${cliente.nombre}`,
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
.lista-tarjetas-clientes {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-top: 8px;
}

.tarjeta-cliente {
  cursor: pointer;
}

.tarjeta-cuerpo {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px 16px;
}

.avatar-cliente {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  border: 2px solid rgba(var(--ion-color-primary-rgb, 56, 128, 255), 0.2);
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--ion-color-step-100, #f2f2f2);
  overflow: hidden;
}

.avatar-cliente img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.icono-avatar-lista {
  font-size: 56px;
  color: var(--ion-color-medium, #92949c);
}

.info-cliente {
  flex: 1;
  min-width: 0;
}

.nombre-cliente {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  color: var(--ion-color-step-900, #0f172a);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.contacto-cliente {
  margin: 3px 0 0;
  font-size: 0.82rem;
  color: var(--vet-muted, #64748b);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.direccion-cliente {
  margin: 4px 0 0;
  font-size: 0.8rem;
  color: var(--ion-color-step-700, #334155);
  display: flex;
  align-items: center;
  gap: 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.icono-pin {
  font-size: 14px;
  color: var(--ion-color-primary, #3880ff);
  flex-shrink: 0;
}

.sin-direccion {
  margin: 4px 0 0;
  font-size: 0.78rem;
  color: var(--ion-color-medium, #92949c);
  font-style: italic;
}

.acciones-cliente {
  flex-shrink: 0;
}

.boton-baja {
  --padding-start: 8px;
  --padding-end: 8px;
}
</style>
