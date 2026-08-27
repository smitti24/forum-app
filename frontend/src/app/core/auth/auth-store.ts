import { Injectable, computed, signal } from '@angular/core';
import { Member } from './member.schema';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly _token = signal<string | null>(null);
  private readonly _member = signal<Member | null>(null);

  readonly token = this._token.asReadonly();
  readonly member = this._member.asReadonly();

  readonly isAuthenticated = computed(() => this._token() !== null);
  readonly isModerator = computed(() => this._member()?.role === 'moderator');
  readonly memberId = computed(() => this._member()?.id ?? null);

  signIn(token: string, member: Member): void {
    this._token.set(token);
    this._member.set(member);
  }

  signOut(): void {
    this._token.set(null);
    this._member.set(null);
  }
}
