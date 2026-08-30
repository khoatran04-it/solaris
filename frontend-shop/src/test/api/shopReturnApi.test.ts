import { describe, it, expect, vi, beforeEach } from 'vitest';
import shopReturnApi from '@/api/shopReturnApi';
import axiosClient from '@/api/axiosClient';

vi.mock('@/api/axiosClient', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 🛒 FRONTEND SHOP - MODULE 13: SALES ORDERS & CUSTOMER RETURNS
 * 🧪 API TEST: shopReturnApi (Shop Return RMA Client API)
 * ============================================================================
 */
describe('Module 13 - shopReturnApi Client', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  // TC01: TẠO PHIẾU YÊU CẦU ĐỔI TRẢ (RMA)
  it('TC01 - create gửi POST request tới /returns với payload đổi trả', async () => {
    const payload = {
      orderCode: 'ORD-20260830-010',
      reason: 'Trái cây bị dập khi vận chuyển',
      items: [
        {
          variantId: 10,
          batchId: 5,
          uoMId: 1,
          returnedQuantity: 2,
          reason: 'Dập nát',
        },
      ],
    };

    const mockResponse = {
      id: 5,
      returnCode: 'RET-20260830-005',
      status: 1,
    };

    (axiosClient.post as any).mockResolvedValue(mockResponse);

    const result = await shopReturnApi.create(payload);

    expect(axiosClient.post).toHaveBeenCalledWith('/returns', payload);
    expect(result).toEqual(mockResponse);
  });

  // TC02: LẤY DANH SÁCH PHIẾU TRẢ PHÂN TRANG
  it('TC02 - getAll gửi GET request tới /returns với params phân trang', async () => {
    const mockPagedResult = {
      items: [{ id: 5, returnCode: 'RET-005' }],
      totalRecords: 1,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    };

    (axiosClient.get as any).mockResolvedValue(mockPagedResult);

    const result = await shopReturnApi.getAll(1, 10);

    expect(axiosClient.get).toHaveBeenCalledWith('/returns', {
      params: { pageIndex: 1, pageSize: 10 },
    });
    expect(result).toEqual(mockPagedResult);
  });

  // TC03: LẤY CHI TIẾT PHIẾU TRẢ THEO MÃ
  it('TC03 - getByCode gửi GET request tới /returns/:returnCode', async () => {
    const mockReturn = {
      id: 5,
      returnCode: 'RET-20260830-005',
      orderCode: 'ORD-20260830-010',
      statusName: 'Chờ tiếp nhận',
      details: [],
    };

    (axiosClient.get as any).mockResolvedValue(mockReturn);

    const result = await shopReturnApi.getByCode('RET-20260830-005');

    expect(axiosClient.get).toHaveBeenCalledWith('/returns/RET-20260830-005');
    expect(result).toEqual(mockReturn);
  });
});
