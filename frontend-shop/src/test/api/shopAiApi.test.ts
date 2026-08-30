import { describe, it, expect, vi, beforeEach } from 'vitest';
import axiosClient from '@/api/axiosClient';
import shopAiApi from '@/api/shopAiApi';

vi.mock('@/api/axiosClient', () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
        put: vi.fn(),
        delete: vi.fn(),
    },
}));

describe('Module 15 - Shop AI API Client (Gemini Chatbot)', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('TC01 - getSessions gửi GET /ai/sessions với sessionToken param', async () => {
        const mockSessions = [
            {
                id: 1,
                sessionToken: 'token-123',
                title: 'Tư vấn nông sản sạch',
                createdAt: '2026-08-30T10:00:00Z',
                updatedAt: '2026-08-30T10:10:00Z',
                totalMessages: 2,
                lastMessage: 'Dạ bơ sáp 034 đang sẵn hàng ạ'
            }
        ];
        (axiosClient.get as any).mockResolvedValue(mockSessions);

        const res = await shopAiApi.getSessions('token-123');

        expect(axiosClient.get).toHaveBeenCalledWith('/ai/sessions', { params: { sessionToken: 'token-123' } });
        expect(res).toHaveLength(1);
        expect(res[0].id).toBe(1);
    });

    it('TC02 - getSessionMessages gửi GET /ai/sessions/{id}/messages', async () => {
        const mockMessages = [
            { id: 1, role: 'user', content: 'Có sầu riêng không?', payloadType: 'none', createdAt: '2026-08-30T10:00:00Z' },
            { id: 2, role: 'model', content: 'Dạ có Sầu Riêng Ri6 ạ!', payloadType: 'product_cards', createdAt: '2026-08-30T10:00:05Z' }
        ];
        (axiosClient.get as any).mockResolvedValue(mockMessages);

        const res = await shopAiApi.getSessionMessages(1, 'token-123');

        expect(axiosClient.get).toHaveBeenCalledWith('/ai/sessions/1/messages', { params: { sessionToken: 'token-123' } });
        expect(res).toHaveLength(2);
        expect(res[1].role).toBe('model');
    });

    it('TC03 - createSession gửi POST /ai/sessions với payload', async () => {
        const payload = { sessionToken: 'tok_abc', title: 'Cuộc trò chuyện mới' };
        const mockResponse = { id: 5, sessionToken: 'tok_abc', title: 'Cuộc trò chuyện mới', totalMessages: 0 };
        (axiosClient.post as any).mockResolvedValue(mockResponse);

        const res = await shopAiApi.createSession(payload);

        expect(axiosClient.post).toHaveBeenCalledWith('/ai/sessions', payload);
        expect(res.id).toBe(5);
    });

    it('TC04 - deleteSession gửi DELETE /ai/sessions/{id}', async () => {
        (axiosClient.delete as any).mockResolvedValue({ success: true });

        const res = await shopAiApi.deleteSession(5, 'tok_abc');

        expect(axiosClient.delete).toHaveBeenCalledWith('/ai/sessions/5', { params: { sessionToken: 'tok_abc' } });
        expect(res.success).toBe(true);
    });

    it('TC05 - sendMessage gửi POST /ai/chat và nhận phản hồi AI', async () => {
        const payload = {
            sessionId: 1,
            sessionToken: 'tok_abc',
            message: 'Tư vấn giúp mình bơ sáp 034'
        };
        const mockResponse = {
            sessionId: 1,
            sessionToken: 'tok_abc',
            title: 'Tư vấn bơ sáp 034',
            messageId: 10,
            content: 'Dạ bơ sáp 034 đạt chuẩn VietGAP hái mới hôm nay ạ!',
            payloadType: 'product_cards',
            createdAt: '2026-08-30T10:15:00Z'
        };
        (axiosClient.post as any).mockResolvedValue(mockResponse);

        const res = await shopAiApi.sendMessage(payload);

        expect(axiosClient.post).toHaveBeenCalledWith('/ai/chat', payload);
        expect(res.content).toContain('bơ sáp 034');
        expect(res.payloadType).toBe('product_cards');
    });

    it('TC06 - confirmOrder gửi POST /ai/confirm-order chốt đơn từ chat', async () => {
        const payload = {
            sessionId: 1,
            items: [
                {
                    variantId: 101,
                    variantCode: 'VAR-BO-01',
                    variantName: 'Bơ Sáp 034 VIP',
                    uoMId: 1,
                    uoMName: 'Kg',
                    quantity: 3,
                    unitPrice: 85000,
                    discountAmount: 0,
                    totalPrice: 255000
                }
            ],
            receiverName: 'Khoa Tran',
            receiverPhone: '0912345678',
            deliveryAddress: '123 Le Loi, Q1, TP.HCM',
            shippingFee: 0,
            paymentMethod: 3
        };
        const mockResponse = {
            orderId: 10,
            orderCode: 'ORD-20260830-999',
            totalAmount: 255000,
            paymentMethodName: 'Cổng VNPay Sandbox',
            paymentUrl: 'https://sandbox.vnpayment.vn/paymentv2/vpcpay.html'
        };
        (axiosClient.post as any).mockResolvedValue(mockResponse);

        const res = await shopAiApi.confirmOrder(payload);

        expect(axiosClient.post).toHaveBeenCalledWith('/ai/confirm-order', payload);
        expect(res.orderCode).toBe('ORD-20260830-999');
        expect(res.paymentUrl).toContain('sandbox.vnpayment.vn');
    });
});
