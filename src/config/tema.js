const CLAVE_TEMA = 'veterinaria_tema';

export function es_tema_oscuro() {
  return localStorage.getItem(CLAVE_TEMA) === 'oscuro';
}

export function aplicar_tema_guardado() {
  const oscuro = es_tema_oscuro();
  document.documentElement.classList.toggle('ion-palette-dark', oscuro);
}

export function alternar_tema() {
  const html = document.documentElement;
  const oscuro = html.classList.toggle('ion-palette-dark');
  localStorage.setItem(CLAVE_TEMA, oscuro ? 'oscuro' : 'claro');
  return oscuro;
}