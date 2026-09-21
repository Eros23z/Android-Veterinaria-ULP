import { reactive } from 'vue';
import { enviar } from '../services/ajax_service';
import { armarConsulta } from '../utils/consulta';

export const clientes_store = reactive({
    clientes: [],
    cliente_actual: null,
    paginacion: { pagina: 1, tamano: 10, total: 0, hay_mas: false },
    busqueda: '',
    cargando: false,
    error: null,

    async cargar_clientes(parametros = {}) {
        this.cargando = true;
        this.error = null;
        try {
            const params = {
                pagina: this.paginacion.pagina,
                tamano: this.paginacion.tamano,
                ...(this.busqueda ? { busqueda: this.busqueda } : {}),
                ...parametros
            };
            const query = armarConsulta(params);
            const respuesta = await enviar(`/api/clientes${query}`);
            
            const items = respuesta.clientes ?? respuesta.items ?? [];
            const pag = (respuesta.pagina && typeof respuesta.pagina === 'object') ? respuesta.pagina : respuesta;

            if (this.paginacion.pagina === 1) {
                this.clientes = items;
            } else {
                this.clientes = [...this.clientes, ...items];
            }
            
            this.paginacion = {
                pagina: pag.pagina ?? 1,
                tamano: pag.tamano ?? 10,
                total: pag.total ?? 0,
                hay_mas: pag.hay_mas ?? false
            };
        } catch (error) {
            this.error = error.message;
        } finally {
            this.cargando = false;
        }
    },

    async buscar(texto) {
        this.busqueda = texto || '';
        this.reiniciar_paginacion();
        await this.cargar_clientes();
    },

    async cargar_mas_clientes() {
        if (this.paginacion.hay_mas && !this.cargando) {
            this.paginacion.pagina++;
            await this.cargar_clientes();
        }
    },
    
    reiniciar_paginacion() {
        this.paginacion.pagina = 1;
        this.clientes = [];
    }
});

