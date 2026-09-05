import { describe, it, expect, vi, beforeEach } from 'vitest';
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import InteractiveOrderCard from '@/components/chat/InteractiveOrderCard';
import shopAiApi from '@/api/shopAiApi';

vi.mock('@/api/shopAiApi', () => ({
    default: {
        confirmOrder: vi.fn(),
    },
}));

describe('Module 15 - InteractiveOrderCard Component (UI Testing in RAM)', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    const mockPayload = {
        title: 'Đơn Hàng Đặt Lại Bơ Sáp',
        previousOrderCode: 'ORD-OLD-123',
        items: [
            {
                variantId: 101,
                variantCode: 'VAR-BO-01',
                variantName: 'Bơ Sáp 034 Đặc Sản',
                uoMId: 1,
                uoMName: 'Kg',
                unitPrice: 100000,
                quantity: 2,
                discountAmount: 0,
                totalPrice: 200000
            }
        ],
        subTotal: 200000,
        totalDiscount: 0,
        shippingFee: 25000,
        totalAmount: 225000,
        isFreeShipping: false,
        freeShippingThreshold: 300000,
        suggestedReceiverName: 'Khoa Tran',
        suggestedReceiverPhone: '0912345678',
        suggestedDeliveryAddress: '123 Le Loi, Quan 1'
    };

    it('TC01 - Render thông tin sản phẩm và tính toán tổng tiền', () => {
        render(<InteractiveOrderCard sessionId={1} payload={mockPayload} />);

        expect(screen.getByText('Đơn Hàng Đặt Lại Bơ Sáp')).toBeInTheDocument();
        expect(screen.getByText('Bơ Sáp 034 Đặc Sản')).toBeInTheDocument();
        expect(screen.getByDisplayValue('Khoa Tran')).toBeInTheDocument();
        expect(screen.getByDisplayValue('0912345678')).toBeInTheDocument();
        expect(screen.getByDisplayValue('123 Le Loi, Quan 1')).toBeInTheDocument();
    });

    it('TC02 - Tăng số lượng sản phẩm và tự động đạt Freeship khi >= 300k', () => {
        render(<InteractiveOrderCard sessionId={1} payload={mockPayload} />);

        // Ban đầu 2kg * 100k = 200k (< 300k -> có phí ship 25k)
        expect(screen.getByText(/Thêm/i)).toBeInTheDocument();

        // Bấm nút '+' để tăng lên 3kg (3kg * 100k = 300k -> Đạt Freeship)
        const plusButton = screen.getByTitle('Tăng số lượng');
        fireEvent.click(plusButton);

        expect(screen.getByText('3')).toBeInTheDocument();
        expect(screen.getByText(/MIỄN PHÍ GIAO HÀNG/i)).toBeInTheDocument();
    });

    it('TC03 - Xác nhận đặt hàng thành công qua COD', async () => {
        (shopAiApi.confirmOrder as any).mockResolvedValue({
            orderId: 99,
            orderCode: 'ORD-20260830-777',
            totalAmount: 225000,
            paymentMethodName: 'Thanh toán khi nhận (COD)'
        });

        render(<InteractiveOrderCard sessionId={1} payload={mockPayload} />);

        // Chọn COD
        const codRadio = screen.getByLabelText(/Khi nhận \(COD\)/i);
        fireEvent.click(codRadio);

        // Bấm Xác nhận
        const submitBtn = screen.getByRole('button', { name: /Xác Nhận Đặt Đơn Này/i });
        fireEvent.click(submitBtn);

        await waitFor(() => {
            expect(shopAiApi.confirmOrder).toHaveBeenCalledWith(
                expect.objectContaining({
                    sessionId: 1,
                    paymentMethod: 1
                })
            );
            expect(screen.getByText('ĐÃ TẠO ĐƠN HÀNG THÀNH CÔNG!')).toBeInTheDocument();
            expect(screen.getByText('ORD-20260830-777')).toBeInTheDocument();
        });
    });
});
