import { Component, OnInit, OnDestroy } from '@angular/core';
import { interval, Subscription } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { ChatService } from '../../services/chat.service';
import { SignalRService } from '../../services/signalr.service';
import { ChatMessage } from '../../models/chat.models';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css']
})
export class ChatComponent implements OnInit, OnDestroy {
  userId: string = '';
  sessionId: string = '';
  status: string = 'initial'; // initial, requesting, queued, active, completed, refused
  assignedAgent: string = '';
  messages: ChatMessage[] = [];
  newMessage: string = '';
  errorMessage: string = '';
  
  private pollSubscription?: Subscription;
  private signalRSubscription?: Subscription;

  constructor(
    private chatService: ChatService,
    private signalRService: SignalRService
  ) { }

  ngOnInit(): void {
    // Generate random user ID for demo
    this.userId = 'User_' + Math.floor(Math.random() * 10000);
  }

  ngOnDestroy(): void {
    this.stopPolling();
    if (this.sessionId) {
      this.signalRService.leaveChatSession(this.sessionId);
      this.signalRService.stopConnection();
    }
  }

  startChat(): void {
    if (!this.userId) {
      this.errorMessage = 'Please enter a user ID';
      return;
    }

    this.status = 'requesting';
    this.errorMessage = '';

    this.chatService.createChatRequest(this.userId).subscribe({
      next: (response) => {
        if (response.message === 'OK' && response.sessionId) {
          this.sessionId = response.sessionId;
          this.status = 'queued';
          this.startPolling();
          this.connectSignalR();
        } else {
          this.status = 'refused';
          this.errorMessage = response.message || 'Chat request failed';
        }
      },
      error: (error) => {
        this.status = 'refused';
        this.errorMessage = error.error?.message || 'Failed to create chat request';
      }
    });
  }

  private startPolling(): void {
    // Poll every 1 second
    this.pollSubscription = interval(1000)
      .pipe(
        switchMap(() => this.chatService.pollSession(this.sessionId))
      )
      .subscribe({
        next: (response) => {
          if (response.status === 'Active' && this.status !== 'active') {
            this.status = 'active';
            this.assignedAgent = response.assignedAgent;
          } else if (response.status === 'Inactive') {
            this.status = 'inactive';
            this.errorMessage = 'Session became inactive due to missed polls';
            this.stopPolling();
          }
        },
        error: (error) => {
          console.error('Polling error:', error);
        }
      });
  }

  private stopPolling(): void {
    if (this.pollSubscription) {
      this.pollSubscription.unsubscribe();
    }
  }

  private connectSignalR(): void {
    this.signalRService.startConnection(this.sessionId)
      .then(() => {
        // Listen for messages
        this.signalRService.messageReceived.subscribe(message => {
          this.messages.push(message);
        });

        // Listen for agent assignment
        this.signalRService.agentAssigned.subscribe(agentName => {
          this.assignedAgent = agentName;
          this.status = 'active';
        });
      })
      .catch(err => {
        console.error('Failed to connect to SignalR:', err);
      });
  }

  sendMessage(): void {
    if (!this.newMessage.trim() || this.status !== 'active') {
      return;
    }

    this.signalRService.sendMessage(
      this.sessionId,
      this.userId,
      this.userId,
      this.newMessage
    );

    this.newMessage = '';
  }

  endChat(): void {
    this.chatService.completeSession(this.sessionId).subscribe({
      next: () => {
        this.status = 'completed';
        this.stopPolling();
        this.signalRService.stopConnection();
      },
      error: (error) => {
        console.error('Error ending chat:', error);
      }
    });
  }

  resetChat(): void {
    this.stopPolling();
    if (this.sessionId) {
      this.signalRService.leaveChatSession(this.sessionId);
      this.signalRService.stopConnection();
    }
    
    this.sessionId = '';
    this.status = 'initial';
    this.assignedAgent = '';
    this.messages = [];
    this.newMessage = '';
    this.errorMessage = '';
  }
}
