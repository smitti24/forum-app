import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom, map } from 'rxjs';
import { API_BASE_URL } from '../../../core/api/api.config';
import { parseWith } from '../../../core/api/parse';
import { AuthStore } from '../../../core/auth/auth-store';
import { MemberSchema } from '../../../core/auth/member.schema';
import { AuthResponseSchema, Login, Register } from './auth.schema';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly store = inject(AuthStore);

  async login(input: Login): Promise<void> {
    const { token, member } = await firstValueFrom(
      this.http
        .post<unknown>(`${this.baseUrl}/auth/login`, input)
        .pipe(map(parseWith(AuthResponseSchema))),
    );
    this.store.signIn(token, member);
  }

  async register(input: Register): Promise<void> {
    const { token, member } = await firstValueFrom(
      this.http
        .post<unknown>(`${this.baseUrl}/auth/register`, input)
        .pipe(map(parseWith(AuthResponseSchema))),
    );
    this.store.signIn(token, member);
  }

  me() {
    return firstValueFrom(
      this.http.get<unknown>(`${this.baseUrl}/auth/me`).pipe(map(parseWith(MemberSchema))),
    );
  }

  logout(): void {
    this.store.signOut();
  }
}
