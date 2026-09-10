const RETARDO_SIMULADO = 600;

const PRODUCTOS = [
  { id: 1, nombre: 'Consulta Veterinaria General', precio: 12000, disponible: true, categoria: 'Atención' },
  { id: 2, nombre: 'Vacuna Antirrábica', precio: 8500, disponible: true, categoria: 'Vacunación' },
  { id: 3, nombre: 'Pipeta Antipulgas Perro Mediano', precio: 7200, disponible: true, categoria: 'Farmacia' },
  { id: 4, nombre: 'Alimento Balanceado Premium 15kg', precio: 45000, disponible: false, categoria: 'Nutrición' },
  { id: 5, nombre: 'Ecografía de Control Abdominal', precio: 18000, disponible: true, categoria: 'Estudios' },
  { id: 6, nombre: 'Desparasitación Interna Felina', precio: 5400, disponible: true, categoria: 'Farmacia' }
];

export function obtener_productos() {
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve({ productos: PRODUCTOS });
    }, RETARDO_SIMULADO);
  });
}