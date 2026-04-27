import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-unit-preview',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './unit-preview.component.html',
  styleUrl: './unit-preview.component.scss',
})
export class UnitPreviewComponent {
  @Input({ required: true }) title = '';
  @Input({ required: true }) summary = '';
  @Input({ required: true }) route: string[] = ['/learn', 'A1'];
}
