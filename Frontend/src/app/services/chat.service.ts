import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ChatSession, ChatRequestDto, ChatStats } from '../models/chat.models';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  createChatRequest(userId: string): Observable<any> {
    const request: ChatRequestDto = { userId };
    return this.http.post<any>(`${this.apiUrl}/chat/request`, request);
  }

  getChatSession(sessionId: string): Observable<ChatSession> {
    return this.http.get<ChatSession>(`${this.apiUrl}/chat/session/${sessionId}`);
  }

  pollSession(sessionId: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/chat/session/${sessionId}/poll`, {});
  }

  completeSession(sessionId: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/chat/session/${sessionId}/complete`, {});
  }

  getStats(): Observable<ChatStats> {
    return this.http.get<ChatStats>(`${this.apiUrl}/chat/stats`);
  }
}
