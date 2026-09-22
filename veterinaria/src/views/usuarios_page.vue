<template>
  <comp-page titulo="Gestión de Usuarios" :mostrar_actualizar="true" @actualizar="cargar">
    <comp-buscador placeholder="Buscar por email..." @buscar="onBuscar" />

    <comp-esqueleto v-if="usuarios_store.cargando && usuarios_store.usuarios.length === 0" />

    <div v-else-if="usuarios_store.usuarios.length === 0" class="ion-padding ion-text-center">
      <p>No se encontraron usuarios registrados.</p>
    </div>

    <comp-lista
      v-else
      :total="usuarios_store.paginacion.total"
      :cantidad_mostrada="usuarios_store.usuarios.length"
      :hay_mas="usuarios_store.paginacion.hay_mas"
      :cargando="usuarios_store.cargando"
      @ver_mas="cargarMas"
    >
      <template #lista>
        <ion-list>
          <ion-item-sliding v-for="u in usuarios_store.usuarios" :key="u.id">
            <ion-item button @click="abrirModalRol(u)">
              <ion-icon slot="start" :icon="personCircleOutline" color="medium"></ion-icon>
              <ion-label>
                <h2>{{ u.email }}</h2>
                <p>Registrado: {{ new Date(u.creado_en).toLocaleDateString() }}</p>
              </ion-label>
              <ion-badge slot="end" :color="obtenerColorRol(u.rol_codigo)">
                {{ u.rol_nombre || 'Sin rol' }}
              </ion-badge>
              <ion-button slot="end" fill="clear" color="danger" @click.stop="confirmarBaja(u)">
                <ion-icon slot="icon-only" :icon="trashOutline"></ion-icon>
              </ion-button>
            </ion-item>

            <ion-item-options side="end">
              <ion-item-option color="primary" @click="abrirModalRol(u)">
                <ion-icon slot="icon-only" :icon="shieldOutline"></ion-icon>
              </ion-item-option>
              <ion-item-option color="danger" @click="confirmarBaja(u)">
                <ion-icon slot="icon-only" :icon="trashOutline"></ion-icon>
              </ion-item-option>
            </ion-item-options>
          </ion-item-sliding>
        </ion-list>
      </template>
    </comp-lista>

    <!-- FAB para dar de alta nuevo usuario -->
    <ion-fab vertical="bottom" horizontal="end" slot="fixed">
      <ion-fab-button @click="abrirModalNuevoUsuario">
        <ion-icon :icon="add"></ion-icon>
      </ion-fab-button>
    </ion-fab>

    <!-- Modal para Asignar / Cambiar Rol -->
    <ion-modal :is-open="modalRolAbierto" @didDismiss="modalRolAbierto = false">
      <ion-header>
        <ion-toolbar color="primary">
          <ion-title>Modificar Rol</ion-title>
          <ion-buttons slot="end">
            <ion-button @click="modalRolAbierto = false">Cerrar</ion-button>
          </ion-buttons>
        </ion-toolbar>
      </ion-header>
      <ion-content class="ion-padding">
        <div v-if="usuarioSeleccionado" class="info-usuario ion-margin-bottom">
          <p><strong>Usuario:</strong> {{ usuarioSeleccionado.email }}</p>
          <p><strong>Rol actual:</strong> {{ usuarioSeleccionado.rol_nombre }}</p>
        </div>

        <ion-list>
          <ion-radio-group v-model="nuevoRolId">
            <ion-list-header>
              <ion-label>Seleccione el nuevo rol:</ion-label>
            </ion-list-header>

            <ion-item>
              <ion-radio :value="null">Sin rol asignado</ion-radio>
            </ion-item>

            <ion-item v-for="r in usuarios_store.roles" :key="r.id">
              <ion-radio :value="r.id">{{ r.nombre }} ({{ r.codigo }})</ion-radio>
            </ion-item>
          </ion-radio-group>
        </ion-list>

        <div v-if="errorModalRol" class="ion-margin-top">
          <ion-note color="danger">{{ errorModalRol }}</ion-note>
        </div>

        <div class="ion-padding-top">
          <ion-button expand="block" shape="round" :disabled="guardandoRol" @click="guardarCambioRol">
            {{ guardandoRol ? 'Guardando...' : 'Aplicar Cambio de Rol' }}
          </ion-button>
        </div>
      </ion-content>
    </ion-modal>

    <!-- Modal para Crear Usuario -->
    <ion-modal :is-open="modalNuevoAbierto" @didDismiss="modalNuevoAbierto = false">
      <ion-header>
        <ion-toolbar color="primary">
          <ion-title>Nuevo Usuario</ion-title>
          <ion-buttons slot="end">
            <ion-button @click="modalNuevoAbierto = false">Cerrar</ion-button>
          </ion-buttons>
        </ion-toolbar>
      </ion-header>
      <ion-content class="ion-padding">
        <div v-if="errorModalNuevo" class="ion-margin-bottom">
          <ion-note color="danger" class="error-note">{{ errorModalNuevo }}</ion-note>
        </div>

        <ion-item>
          <ion-label position="stacked">Correo Electrónico *</ion-label>
          <ion-input v-model="formNuevo.email" type="email" placeholder="usuario@veterinaria.local"></ion-input>
        </ion-item>

        <ion-item>
          <ion-label position="stacked">Contraseña *</ion-label>
          <ion-input v-model="formNuevo.password" type="password" placeholder="••••••••"></ion-input>
        </ion-item>

        <ion-item>
          <ion-label position="stacked">Rol Inicial</ion-label>
          <ion-select v-model="formNuevo.rol_id" placeholder="Seleccionar rol">
            <ion-select-option :value="null">Sin rol (asignar luego)</ion-select-option>
            <ion-select-option v-for="r in usuarios_store.roles" :key="r.id" :value="r.id">
              {{ r.nombre }} ({{ r.codigo }})
            </ion-select-option>
          </ion-select>
        </ion-item>

        <div class="ion-padding-top">
          <ion-button expand="block" shape="round" :disabled="guardandoNuevo" @click="guardarNuevoUsuario">
            {{ guardandoNuevo ? 'Creando...' : 'Crear Usuario' }}
          </ion-button>
        </div>
      </ion-content>
    </ion-modal>
  </comp-page>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import {
  IonList,
  IonListHeader,
  IonItem,
  IonItemSliding,
  IonItemOptions,
  IonItemOption,
  IonLabel,
  IonBadge,
  IonButton,
  IonIcon,
  IonFab,
  IonFabButton,
  IonModal,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonContent,
  IonRadioGroup,
  IonRadio,
  IonInput,
  IonSelect,
  IonSelectOption,
  IonNote,
  alertController,
} from '@ionic/vue';
import {
  personCircleOutline,
  shieldOutline,
  trashOutline,
  add,
} from 'ionicons/icons';
import CompPage from '@/components/estructura/comp_page.vue';
import CompEsqueleto from '@/components/base/comp_esqueleto.vue';
import CompBuscador from '@/components/base/comp_buscador.vue';
import CompLista from '@/components/base/comp_lista.vue';
import { usuarios_store } from '@/stores/usuarios_store';

const modalRolAbierto = ref(false);
const modalNuevoAbierto = ref(false);
const usuarioSeleccionado = ref(null);
const nuevoRolId = ref(null);
const guardandoRol = ref(false);
const errorModalRol = ref(null);

const formNuevo = ref({
  email: '',
  password: '',
  rol_id: null,
});
const guardandoNuevo = ref(false);
const errorModalNuevo = ref(null);

function obtenerColorRol(rolCodigo) {
  switch (rolCodigo) {
    case 'ADMIN':
      return 'success';
    case 'VETERINARIO':
      return 'primary';
    default:
      return 'warning';
  }
}

async function cargar(event = null) {
  usuarios_store.reiniciar_paginacion();
  await Promise.all([
    usuarios_store.cargar_usuarios(),
    usuarios_store.cargar_roles(),
  ]);
  if (event?.target?.complete) {
    event.target.complete();
  }
}

async function cargarMas() {
  await usuarios_store.cargar_mas_usuarios();
}

async function onBuscar(termino) {
  await usuarios_store.buscar(termino);
}

function abrirModalRol(usuario) {
  usuarioSeleccionado.value = usuario;
  nuevoRolId.value = usuario.rol_id ?? null;
  errorModalRol.value = null;
  modalRolAbierto.value = true;
}

async function guardarCambioRol() {
  if (!usuarioSeleccionado.value) return;

  guardandoRol.value = true;
  errorModalRol.value = null;

  try {
    await usuarios_store.actualizar_rol(usuarioSeleccionado.value.id, nuevoRolId.value);
    modalRolAbierto.value = false;
  } catch (error) {
    errorModalRol.value = error.mensaje || error.message || 'Error al actualizar rol.';
  } finally {
    guardandoRol.value = false;
  }
}

function abrirModalNuevoUsuario() {
  formNuevo.value = { email: '', password: '', rol_id: null };
  errorModalNuevo.value = null;
  modalNuevoAbierto.value = true;
}

async function guardarNuevoUsuario() {
  if (!formNuevo.value.email || !formNuevo.value.password) {
    errorModalNuevo.value = 'El email y la contraseña son obligatorios.';
    return;
  }

  guardandoNuevo.value = true;
  errorModalNuevo.value = null;

  try {
    await usuarios_store.crear_usuario(formNuevo.value);
    modalNuevoAbierto.value = false;
  } catch (error) {
    errorModalNuevo.value = error.mensaje || error.message || 'Error al crear usuario.';
  } finally {
    guardandoNuevo.value = false;
  }
}

async function confirmarBaja(usuario) {
  const alerta = await alertController.create({
    header: 'Dar de baja usuario',
    message: `¿Desea dar de baja al usuario "${usuario.email}"?`,
    buttons: [
      {
        text: 'Cancelar',
        role: 'cancel',
      },
      {
        text: 'Dar de baja',
        role: 'destructive',
        handler: async () => {
          try {
            await usuarios_store.eliminar_usuario(usuario.id);
          } catch (e) {
            console.error('Error al dar de baja usuario:', e);
          }
        },
      },
    ],
  });

  await alerta.present();
}

onMounted(() => cargar());
</script>

<style scoped>
.info-usuario {
  background: var(--ion-color-step-100, #f0f0f0);
  padding: 12px;
  border-radius: 8px;
}

.error-note {
  display: block;
  padding: 8px 12px;
  background: rgba(var(--ion-color-danger-rgb, 235, 68, 90), 0.1);
  border-radius: 8px;
  font-weight: 500;
}
</style>
