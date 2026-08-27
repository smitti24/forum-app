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

  login(input: Login) {
    return this.authenticate(`${this.baseUrl}/auth/login`, input);
  }

  register(input: Register) {
    return this.authenticate(`${this.baseUrl}/auth/register`, input);
  }

  me() {
    return firstValueFrom(
      this.http.get<unknown>(`${this.baseUrl}/auth/me`).pipe(map(parseWith(MemberSchema))),
    );
  }

  logout(): void {
    this.store.signOut();
  }

  private async authenticate(url: string, body: Login | Register): Promise<void> {
    const auth = await firstValueFrom(
      this.http.post<unknown>(url, body).pipe(map(parseWith(AuthResponseSchema))),
    );

    this.store.setToken(auth.token, auth.expiresAt);
    this.store.setMember(auth.member);
  }
}
