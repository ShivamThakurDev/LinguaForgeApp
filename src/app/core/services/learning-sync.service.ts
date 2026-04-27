import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LearningSyncService {
  refreshTick = signal(0);

  notifyUpdate(): void {
    this.refreshTick.update((value) => value + 1);
  }
}
