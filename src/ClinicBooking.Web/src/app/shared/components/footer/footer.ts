import { Component } from '@angular/core';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <footer class="footer-wrapper">
      <div class="container footer-content">
        <p>© 2026 {{ 'nav.brand' | translate }}. All rights reserved.</p>
        <p class="tech-stack">ASP.NET Core .NET 10 • Clean Architecture • Angular • Bilingual Arabic & English</p>
      </div>
    </footer>
  `,
  styles: [`
    .footer-wrapper {
      margin-top: auto;
      background-color: var(--bg-card);
      border-top: 1px solid var(--border);
      padding: 2rem 0;
      color: var(--text-muted);
      font-size: 0.875rem;
    }
    .footer-content {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.5rem;
      text-align: center;
    }
    .tech-stack {
      font-size: 0.775rem;
      color: var(--text-light);
    }
  `]
})
export class FooterComponent {}
