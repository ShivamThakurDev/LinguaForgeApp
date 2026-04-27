import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-quick-actions',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './quick-actions.component.html',
  styleUrl: './quick-actions.component.scss',
})
export class QuickActionsComponent {
  protected readonly actions = [
    { label: 'Open A1 map', route: ['/learn', 'A1'] },
    { label: 'Open B1 map', route: ['/learn', 'B1'] },
    { label: 'Check progress', route: ['/progress'] },
  ];
}
