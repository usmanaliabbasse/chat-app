import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { ChatMessage } from '../models/chat.models';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection?: signalR.HubConnection;
  public messageReceived = new Subject<ChatMessage>();
  public agentAssigned = new Subject<string>();

  constructor() { }

  public startConnection(sessionId: string): Promise<void> {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect()
      .build();

    return this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR Connected');
        this.joinChatSession(sessionId);
        this.addMessageListener();
        this.addAgentAssignedListener();
      })
      .catch(err => {
        console.error('Error connecting to SignalR:', err);
        throw err;
      });
  }

  public stopConnection(): Promise<void> {
    if (this.hubConnection) {
      return this.hubConnection.stop();
    }
    return Promise.resolve();
  }

  private joinChatSession(sessionId: string): void {
    if (this.hubConnection) {
      this.hubConnection.invoke('JoinChatSession', sessionId)
        .catch(err => console.error('Error joining chat session:', err));
    }
  }

  public leaveChatSession(sessionId: string): void {
    if (this.hubConnection) {
      this.hubConnection.invoke('LeaveChatSession', sessionId)
        .catch(err => console.error('Error leaving chat session:', err));
    }
  }

  public sendMessage(sessionId: string, senderId: string, senderName: string, message: string): void {
    if (this.hubConnection) {
      this.hubConnection.invoke('SendMessage', sessionId, senderId, senderName, message)
        .catch(err => console.error('Error sending message:', err));
    }
  }

  private addMessageListener(): void {
    if (this.hubConnection) {
      this.hubConnection.on('ReceiveMessage', (data: any) => {
        const message: ChatMessage = {
          id: data.id,
          senderId: data.senderId,
          senderName: data.senderName,
          message: data.message,
          timestamp: new Date(data.timestamp)
        };
        this.messageReceived.next(message);
      });
    }
  }

  private addAgentAssignedListener(): void {
    if (this.hubConnection) {
      this.hubConnection.on('AgentAssigned', (agentName: string) => {
        this.agentAssigned.next(agentName);
      });
    }
  }
}
