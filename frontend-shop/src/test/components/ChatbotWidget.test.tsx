import { describe, it, expect, vi, beforeEach } from 'vitest';
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ChatbotWidget from '@/components/chat/ChatbotWidget';
import shopAiApi from '@/api/shopAiApi';

vi.mock('@/api/shopAiApi', () => ({
    default: {
        getSessions: vi.fn(),
        getSessionMessages: vi.fn(),
        createSession: vi.fn(),
        deleteSession: vi.fn(),
        sendMessage: vi.fn(),
        confirmOrder: vi.fn(),
    },
}));

describe('Module 15 - ChatbotWidget Component (UI Testing in RAM)', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        // Mock scrollIntoView in jsdom
        window.HTMLElement.prototype.scrollIntoView = vi.fn();
    });

    it('TC01 - Render nút mở Chatbot dạng nổi (Floating Button)', () => {
        render(<ChatbotWidget />);

        const chatButton = screen.getByRole('button', { name: /Trợ Lý Nông Sản AI/i });
        expect(chatButton).toBeInTheDocument();
        expect(screen.getByText(/Trợ Lý Nông Sản AI/i)).toBeInTheDocument();
        expect(screen.getByText(/3.5 Flash/i)).toBeInTheDocument();
    });

    it('TC02 - Mở khung Chat khi click vào nút và tải phiên hội thoại', async () => {
        (shopAiApi.getSessions as any).mockResolvedValue([
            { id: 1, title: 'Tư vấn bơ 034', totalMessages: 2, updatedAt: '2026-08-30' }
        ]);
        (shopAiApi.getSessionMessages as any).mockResolvedValue([
            { id: 10, role: 'user', content: 'Bơ sáp có ngon không?' },
            { id: 11, role: 'model', content: 'Dạ bơ sáp 034 dẻo béo thơm ngon ạ!' }
        ]);

        render(<ChatbotWidget />);

        const openBtn = screen.getByRole('button', { name: /Trợ Lý Nông Sản AI/i });
        fireEvent.click(openBtn);

        await waitFor(() => {
            expect(screen.getByText('Solaris AI Commerce')).toBeInTheDocument();
            expect(screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i)).toBeInTheDocument();
        });
    });

    it('TC03 - Gửi tin nhắn và render câu trả lời từ AI trong RAM', async () => {
        (shopAiApi.getSessions as any).mockResolvedValue([]);
        (shopAiApi.createSession as any).mockResolvedValue({ id: 1, title: 'Cuộc trò chuyện mới' });
        (shopAiApi.sendMessage as any).mockResolvedValue({
            sessionId: 1,
            messageId: 100,
            content: 'Dạ Solaris có sầu riêng Ri6 tươi ngon!',
            payloadType: 'none',
            createdAt: new Date().toISOString()
        });

        render(<ChatbotWidget />);

        // Mở chat
        fireEvent.click(screen.getByRole('button', { name: /Trợ Lý Nông Sản AI/i }));

        await waitFor(() => {
            expect(screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i)).toBeInTheDocument();
        });

        const input = screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i);
        fireEvent.change(input, { target: { value: 'Có sầu riêng Ri6 không?' } });

        const form = input.closest('form')!;
        fireEvent.submit(form);

        await waitFor(() => {
            expect(shopAiApi.sendMessage).toHaveBeenCalledWith(
                expect.objectContaining({ message: 'Có sầu riêng Ri6 không?' })
            );
            expect(screen.getByText(/Dạ Solaris có sầu riêng Ri6 tươi ngon!/i)).toBeInTheDocument();
        });
    });

    it('TC04 - Render Thẻ Đơn Hàng Tương Tác khi AI trả về interactive_order payload', async () => {
        (shopAiApi.getSessions as any).mockResolvedValue([]);
        (shopAiApi.createSession as any).mockResolvedValue({ id: 1, title: 'Cuộc trò chuyện mới' });
        (shopAiApi.sendMessage as any).mockResolvedValue({
            sessionId: 1,
            messageId: 101,
            content: 'Em đã chuẩn bị sẵn đơn hàng cho bạn:',
            payloadType: 'interactive_order',
            payload: {
                title: 'Đơn Hàng Gợi Ý Bơ 034',
                items: [
                    {
                        variantId: 1,
                        variantName: 'Bơ Sáp 034 VIP',
                        unitPrice: 85000,
                        quantity: 2,
                        discountAmount: 0,
                        totalPrice: 170000,
                        uoMName: 'Kg'
                    }
                ],
                subTotal: 170000,
                totalDiscount: 0,
                shippingFee: 25000,
                totalAmount: 195000,
                isFreeShipping: false
            }
        });

        render(<ChatbotWidget />);

        fireEvent.click(screen.getByRole('button', { name: /Trợ Lý Nông Sản AI/i }));

        await waitFor(() => {
            expect(screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i)).toBeInTheDocument();
        });

        const input = screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i);
        fireEvent.change(input, { target: { value: 'Lên đơn 2kg bơ 034' } });

        const form = input.closest('form')!;
        fireEvent.submit(form);

        await waitFor(() => {
            expect(screen.getByText('Đơn Hàng Gợi Ý Bơ 034')).toBeInTheDocument();
            expect(screen.getByText('Bơ Sáp 034 VIP')).toBeInTheDocument();
        });
    });
});
