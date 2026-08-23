import axiosClient from './axiosClient';
import { 
    ChatSession, 
    ChatMessage, 
    ConfirmInteractiveOrderPayload, 
    ConfirmInteractiveOrderResponse 
} from '@/types/chat';

export interface AiSendMessagePayload {
    sessionId?: number;
    sessionToken?: string;
    message: string;
}

export interface AiChatResponse {
    sessionId: number;
    sessionToken: string;
    title: string;
    messageId: number;
    content: string;
    payloadType: 'none' | 'product_cards' | 'interactive_order' | 'order_success';
    payload?: any;
    createdAt: string;
}

const shopAiApi = {
    getSessions: (sessionToken?: string) =>
        axiosClient.get<ChatSession[]>('/ai/sessions', { params: { sessionToken } }),

    getSessionMessages: (sessionId: number, sessionToken?: string) =>
        axiosClient.get<ChatMessage[]>(`/ai/sessions/${sessionId}/messages`, { params: { sessionToken } }),

    createSession: (payload?: { sessionToken?: string; title?: string }) =>
        axiosClient.post<ChatSession>('/ai/sessions', payload),

    deleteSession: (sessionId: number, sessionToken?: string) =>
        axiosClient.delete<{ success: boolean }>(`/ai/sessions/${sessionId}`, { params: { sessionToken } }),

    sendMessage: (payload: AiSendMessagePayload) =>
        axiosClient.post<AiChatResponse>('/ai/chat', payload),

    confirmOrder: (payload: ConfirmInteractiveOrderPayload) =>
        axiosClient.post<ConfirmInteractiveOrderResponse>('/ai/confirm-order', payload),
};

export default shopAiApi;
