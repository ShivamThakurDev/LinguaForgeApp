import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Lesson } from '../../shared/models/learning.models';

@Component({
  selector: 'app-lesson-node',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './lesson-node.component.html',
  styleUrl: './lesson-node.component.scss',
})
export class LessonNodeComponent {
  @Input({ required: true }) lesson!: Lesson;
  @Input({ required: true }) stepLabel = '';
  @Input() lane: 'left' | 'right' = 'left';
  @Input() status: 'completed' | 'active' | 'locked' = 'locked';
  @Input() xpReward = 20;
  @Input() durationMinutes = 4;
}
