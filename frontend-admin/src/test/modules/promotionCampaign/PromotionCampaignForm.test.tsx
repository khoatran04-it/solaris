import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import PromotionCampaignForm from '../../../pages/promotionCampaign/PromotionCampaignForm';
import { promotionCampaignApi } from '../../../api/promotionCampaignApi';
import { productVariantApi } from '../../../api/productVariantApi';

// Mock APIs
vi.mock('../../../api/promotionCampaignApi', () => ({
  promotionCampaignApi: {
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    addVariants: vi.fn(),
  },
}));

vi.mock('../../../api/productVariantApi', () => ({
  productVariantApi: {
    getAllList: vi.fn(),
  },
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

/**
 * ============================================================================
 * 📦 MODULE 07: PROMOTION CAMPAIGNS
 * 🧪 COMPONENT TEST: PromotionCampaignForm (Thêm / Sửa Chiến Dịch Khuyến Mãi)
 * ============================================================================
 */
describe('Module 07 - PromotionCampaignForm Component', () => {
  const mockVariants = [
    {
      id: 101,
      code: 'SKU-XOAI-500G',
      name: 'Xoài Cát Hòa Lộc 500g',
      imagePath: '/images/xoai-500g.png',
      prices: [
        {
          id: 1,
          uoMName: 'Kg',
          price: 60000,
          isDefault: true,
        },
      ],
    },
    {
      id: 102,
      code: 'SKU-XOAI-1KG',
      name: 'Xoài Cát Hòa Lộc 1kg',
      imagePath: null,
      prices: [
        {
          id: 2,
          uoMName: 'Hộp',
          price: 120000,
          isDefault: true,
        },
      ],
    },
  ];

  const mockCampaignDetail = {
    id: 1,
    name: 'Flash Sale Cuối Tuần',
    description: 'Chương trình ưu đãi giảm giá',
    isPercentage: true,
    discountValue: 15,
    startDate: '2026-08-20T00:00:00Z',
    endDate: '2026-08-25T23:59:59Z',
    isActive: true,
    appliedVariants: [
      {
        variantId: 101,
        variantCode: 'SKU-XOAI-500G',
        variantName: 'Xoài Cát Hòa Lộc 500g',
        productName: 'Xoài Cát Hòa Lộc',
        defaultPrice: 60000,
        defaultUoMName: 'Kg',
      },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (productVariantApi.getAllList as any).mockResolvedValue(mockVariants);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  it('TC01 - Render form tạo mới với các tab và giá trị mặc định', async () => {
    render(
      <MemoryRouter initialEntries={['/promotions/create']}>
        <Routes>
          <Route path="/promotions/create" element={<PromotionCampaignForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Tạo Chiến Dịch Khuyến Mãi')).toBeInTheDocument();
    expect(screen.getByText('THÔNG TIN CƠ BẢN')).toBeInTheDocument();
    expect(screen.getByText('SẢN PHẨM ÁP DỤNG (0)')).toBeInTheDocument();
    expect(screen.getByText('Thiết Lập Chiến Dịch')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nhập tên chiến dịch khuyến mãi...')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nhập giá trị giảm...')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: CHUYỂN ĐỔI TABS VÀ CHỌN SẢN PHẨM
  it('TC02 - Chuyển đổi giữa các tab và cho phép chọn sản phẩm biến thể áp dụng', async () => {
    render(
      <MemoryRouter initialEntries={['/promotions/create']}>
        <Routes>
          <Route path="/promotions/create" element={<PromotionCampaignForm />} />
        </Routes>
      </MemoryRouter>
    );

    // Chuyển sang Tab 2 bằng cách nhấn nút "Tiếp tục chọn Sản phẩm"
    const nextBtn = screen.getByRole('button', { name: /Tiếp tục chọn Sản phẩm/i });
    fireEvent.click(nextBtn);

    await waitFor(() => {
      expect(screen.getByText('Danh Sách Biến Thể')).toBeInTheDocument();
      expect(screen.getByText('Xoài Cát Hòa Lộc 500g')).toBeInTheDocument();
      expect(screen.getByText('Xoài Cát Hòa Lộc 1kg')).toBeInTheDocument();
    });

    // Chọn variant đầu tiên
    const variantItem = screen.getByText('Xoài Cát Hòa Lộc 500g');
    fireEvent.click(variantItem);

    // Tab label cập nhật số lượng
    await waitFor(() => {
      expect(screen.getByText('SẢN PHẨM ÁP DỤNG (1)')).toBeInTheDocument();
    });

    // Nhấn nút "Chọn tất cả"
    const selectAllBtn = screen.getByRole('button', { name: /Chọn tất cả/i });
    fireEvent.click(selectAllBtn);

    await waitFor(() => {
      expect(screen.getByText('SẢN PHẨM ÁP DỤNG (2)')).toBeInTheDocument();
      expect(screen.getByText(/Bỏ chọn tất cả/i)).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC03: TẠO MỚI CHIẾN DỊCH THÀNH CÔNG
  it('TC03 - Tạo mới chiến dịch khuyến mãi thành công và điều hướng về trang danh sách', async () => {
    (promotionCampaignApi.create as any).mockResolvedValue({ id: 10 });

    render(
      <MemoryRouter initialEntries={['/promotions/create']}>
        <Routes>
          <Route path="/promotions/create" element={<PromotionCampaignForm />} />
        </Routes>
      </MemoryRouter>
    );

    // Điền thông tin ở Tab 1
    const nameInput = screen.getByPlaceholderText('Nhập tên chiến dịch khuyến mãi...');
    fireEvent.change(nameInput, { target: { value: 'Mùa Thu Hoạch Xoài' } });

    const discountInput = screen.getByPlaceholderText('Nhập giá trị giảm...');
    fireEvent.change(discountInput, { target: { value: '25' } });

    // Sang Tab 2
    const nextBtn = screen.getByRole('button', { name: /Tiếp tục chọn Sản phẩm/i });
    fireEvent.click(nextBtn);

    await waitFor(() => {
      expect(screen.getByText('Xoài Cát Hòa Lộc 500g')).toBeInTheDocument();
    });

    // Chọn variant 101
    fireEvent.click(screen.getByText('Xoài Cát Hòa Lộc 500g'));

    // Submit form ở Tab 2
    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(promotionCampaignApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'Mùa Thu Hoạch Xoài',
          discountValue: 25,
          isPercentage: true,
          variantIds: [101],
        })
      );
    });
  });
  // #endregion

  // #region TC04: CHỈNH SỬA CHIẾN DỊCH (EDIT MODE)
  it('TC04 - Load dữ liệu chi tiết và cập nhật thành công ở chế độ chỉnh sửa', async () => {
    (promotionCampaignApi.getById as any).mockResolvedValue(mockCampaignDetail);
    (promotionCampaignApi.update as any).mockResolvedValue({});
    (promotionCampaignApi.addVariants as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/promotions/edit/1']}>
        <Routes>
          <Route path="/promotions/edit/:id" element={<PromotionCampaignForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(promotionCampaignApi.getById).toHaveBeenCalledWith(1);
      expect(screen.getByDisplayValue('Flash Sale Cuối Tuần')).toBeInTheDocument();
      expect(screen.getByText('SẢN PHẨM ÁP DỤNG (1)')).toBeInTheDocument();
    });

    // Thay đổi tên chiến dịch
    const nameInput = screen.getByPlaceholderText('Nhập tên chiến dịch khuyến mãi...');
    fireEvent.change(nameInput, { target: { value: 'Flash Sale Cuối Tuần - Đã Sửa' } });

    // Sang Tab 2 và submit cập nhật
    const nextBtn = screen.getByRole('button', { name: /Tiếp tục chọn Sản phẩm/i });
    fireEvent.click(nextBtn);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /LƯU THAY ĐỔI/i })).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(promotionCampaignApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          name: 'Flash Sale Cuối Tuần - Đã Sửa',
        })
      );
      expect(promotionCampaignApi.addVariants).toHaveBeenCalledWith(1, {
        variantIds: [101],
      });
    });
  });
  // #endregion

  // #region TC05: VALIDATION KHI SUBMIT LỖI
  it('TC05 - Hiển thị toast cảnh báo khi submit với tên trống hoặc mức giảm không hợp lệ', async () => {
    render(
      <MemoryRouter initialEntries={['/promotions/create']}>
        <Routes>
          <Route path="/promotions/create" element={<PromotionCampaignForm />} />
        </Routes>
      </MemoryRouter>
    );

    // Chuyển sang tab 2 và submit khi chưa nhập tên
    const nextBtn = screen.getByRole('button', { name: /Tiếp tục chọn Sản phẩm/i });
    fireEvent.click(nextBtn);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /TẠO MỚI/i })).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText(/Vui lòng nhập tên chiến dịch/i)).toBeInTheDocument();
      expect(promotionCampaignApi.create).not.toHaveBeenCalled();
    });
  });
  // #endregion
});
