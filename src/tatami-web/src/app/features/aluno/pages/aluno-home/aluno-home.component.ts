import { Component } from '@angular/core';

@Component({
  selector: 'app-aluno-home',
  template: `
    <section class="home">
      <h1>Área do aluno</h1>
      <p>Perfil concluído. Em breve: frequência e treinos.</p>
    </section>
  `,
  styles: `
    .home {
      background: #fff;
      border-radius: 1rem;
      padding: 1.5rem;
      box-shadow: 0 8px 24px rgba(20, 40, 30, 0.06);
    }
    h1 {
      margin: 0 0 0.5rem;
      color: #163528;
    }
    p {
      margin: 0;
      color: #5b6b63;
    }
  `,
})
export class AlunoHomeComponent {}
