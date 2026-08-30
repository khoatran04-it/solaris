import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import KetQuaThanhToanPage from '@/app/thanh-toan/ket-qua/page';
import shopPaymentApi from '@/api/shopPaymentApi';

const mockSearchParams = vi.fn();
vi.mock('next/navigation', () => ({
    useSearchParams: () => mockSearchParams(),
    useRouter: () => ({ push: vi.fn() }),
}));

vi.mock('@/api/shopPaymentApi', () => ({
    default: {
        getVnPayCallback: vi.fn(),
        createVnPayUrl: vi.fn(),
    },
}));

describe('Module 14 - KetQuaThanhToanPage Component (VNPay Return)', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('TC01 - Hiển thị trạng thái Thanh Toán Thành Công khi VNPay xác nhận mã 00', async () => {
        const queryParams = new URLSearchParams({
            vnp_Amount: '25000000',
            vnp_BankCode: 'NCB',
            vnp_ResponseCode: '00',
            vnp_TransactionNo: '14567890',
            vnp_TxnRef: 'ORD-20260830-001',
        });
        mockSearchParams.mockReturnValue(queryParams);

        (shopPaymentApi.getVnPayCallback as any).mockResolvedValue({
            isSuccess: true,
            orderCode: 'ORD-20260830-001',
            amount: 250000,
            transactionNo: '14567890',
            bankCode: 'NCB',
            message: 'Thanh toán thành công'
        });

        render(<KetQuaThanhToanPage />);

        await waitFor(() => {
            expect(screen.getByText('Thanh Toán Thành Công!')).toBeInTheDocument();
            expect(screen.getByText('ORD-20260830-001')).toBeInTheDocument();
            expect(screen.getByText('14567890')).toBeInTheDocument();
            expect(screen.getByText('NCB')).toBeInTheDocument();
            expect(screen.getByText('Xem Chi Tiết Đơn Hàng')).toBeInTheDocument();
        });
    });

    it('TC02 - Hiển thị trạng thái Thanh Toán Không Thành Công khi giao dịch bị hủy hoặc lỗi', async () => {
        const queryParams = new URLSearchParams({
            vnp_Amount: '25000000',
            vnp_ResponseCode: '24', // Người dùng hủy giao dịch
            vnp_TxnRef: 'ORD-20260830-002',
        });
        mockSearchParams.mockReturnValue(queryParams);

        (shopPaymentApi.getVnPayCallback as any).mockResolvedValue({
            isSuccess: false,
            orderCode: 'ORD-20260830-002',
            amount: 250000,
            message: 'Giao dịch bị hủy bởi khách hàng'
        });

        render(<KetQuaThanhToanPage />);

        await waitFor(() => {
            expect(screen.getByText('Thanh Toán Không Thành Công')).toBeInTheDocument();
            expect(screen.getByText('Giao dịch bị hủy hoặc xảy ra lỗi trong quá trình xử lý qua VNPay.')).toBeInTheDocument();
            expect(screen.getByText('Tiếp Tục Mua Sắm')).toBeInTheDocument();
        });
    });
});
