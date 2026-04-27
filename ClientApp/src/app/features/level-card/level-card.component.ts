import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-level-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './level-card.component.html',
  styleUrl: './level-card.component.scss',
})
export class LevelCardComponent {
  @Input({ required: true }) code = '';
  @Input({ required: true }) title = '';
  @Input({ required: true }) summary = '';
  @Input() accent = 'sunrise';
}
