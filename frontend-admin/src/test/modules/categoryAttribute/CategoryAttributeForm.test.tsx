import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import CategoryAttributeForm from '../../../pages/categoryAttribute/CategoryAttributeForm';
import { categoryAttributeApi } from '../../../api/categoryAttributeApi';
import { productCategoryApi } from '../../../api/productCategoryApi';
import { attributeDefinitionApi } from '../../../api/attributeDefinitionApi';

// Mock APIs
vi.mock('../../../api/categoryAttributeApi', () => ({
  categoryAttributeApi: {
    getAllList: vi.fn(),
    getById: vi.fn(),
    getByCategoryId: vi.fn(),
    sync: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

vi.mock('../../../api/productCategoryApi', () => ({
  productCategoryApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/attributeDefinitionApi', () => ({
  attributeDefinitionApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 4: CATEGORY ATTRIBUTE (BULK MATRIX)
 * COMPONENT TEST: CategoryAttributeForm (Ma Trận Cấu Hình Thuộc Tính Danh Mục)
 * ============================================================================
 */
describe('Module 04 - CategoryAttributeForm Bulk Matrix Component', () => {
  const mockCategories = [
    { id: 1, name: 'Bơ Sáp & Sầu Riêng', groupName: 'Trái Cây Đặc Sản' },
    { id: 2, name: 'Rau Củ Hữu Cơ', groupName: 'Rau Củ Sạch' },
  ];

  const mockAttributes = [
    { id: 10, name: 'Độ ngọt (Brix)', code: 'ATTR_BRIX', dataType: 'Number' },
    { id: 11, name: 'Xuất xứ / Vùng trồng', code: 'ATTR_ORIGIN', dataType: 'Text' },
    { id: 12, name: 'Chứng nhận chất lượng', code: 'ATTR_CERT', dataType: 'Select' },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (productCategoryApi.getAllList as any).mockResolvedValue(mockCategories);
    (attributeDefinitionApi.getAllList as any).mockResolvedValue(mockAttributes);
    (categoryAttributeApi.getByCategoryId as any).mockResolvedValue([]);
    (categoryAttributeApi.sync as any).mockResolvedValue({ message: 'Success' });
  });

  // #region TC01: RENDER MA TRẬN
  it('TC01 - Render đầy đủ giao diện ma trận cấu hình thuộc tính', async () => {
    render(
      <MemoryRouter initialEntries={['/category-attributes/create']}>
        <Routes>
          <Route path="/category-attributes/create" element={<CategoryAttributeForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Ma Trận Cấu Hình Thuộc Tính Danh Mục')).toBeInTheDocument();
    expect(screen.getByText('-- Chọn danh mục sản phẩm để cấu hình --')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: CHỌN DANH MỤC VÀ HIỂN THỊ CÁC THUỘC TÍNH
  it('TC02 - Chọn danh mục sản phẩm sẽ hiển thị danh sách các thẻ thuộc tính', async () => {
    render(
      <MemoryRouter initialEntries={['/category-attributes/create']}>
        <Routes>
          <Route path="/category-attributes/create" element={<CategoryAttributeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('-- Chọn danh mục sản phẩm để cấu hình --')).toBeInTheDocument();
    });

    // Mở dropdown chọn danh mục
    const selectTrigger = screen.getByText('-- Chọn danh mục sản phẩm để cấu hình --');
    fireEvent.click(selectTrigger);

    await waitFor(() => {
      expect(screen.getByText('Bơ Sáp & Sầu Riêng (Trái Cây Đặc Sản)')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Bơ Sáp & Sầu Riêng (Trái Cây Đặc Sản)'));

    await waitFor(() => {
      expect(categoryAttributeApi.getByCategoryId).toHaveBeenCalledWith(1);
      expect(screen.getByText('Độ ngọt (Brix)')).toBeInTheDocument();
      expect(screen.getByText('Xuất xứ / Vùng trồng')).toBeInTheDocument();
      expect(screen.getByText('Chứng nhận chất lượng')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC03: TÍCH CHỌN THUỘC TÍNH VÀ ĐỔI CỜ BẮT BUỘC
  it('TC03 - Tích chọn thuộc tính và chuyển đổi trạng thái Bắt buộc / Tùy chọn', async () => {
    render(
      <MemoryRouter initialEntries={['/category-attributes/create']}>
        <Routes>
          <Route path="/category-attributes/create" element={<CategoryAttributeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('-- Chọn danh mục sản phẩm để cấu hình --')).toBeInTheDocument();
    });

    // Mở dropdown chọn danh mục
    const selectTrigger = screen.getByText('-- Chọn danh mục sản phẩm để cấu hình --');
    fireEvent.click(selectTrigger);

    await waitFor(() => {
      expect(screen.getByText('Bơ Sáp & Sầu Riêng (Trái Cây Đặc Sản)')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Bơ Sáp & Sầu Riêng (Trái Cây Đặc Sản)'));

    await waitFor(() => {
      expect(screen.getByText('Độ ngọt (Brix)')).toBeInTheDocument();
    });

    // Tích chọn Card "Độ ngọt (Brix)"
    const brixCard = screen.getByText('Độ ngọt (Brix)');
    fireEvent.click(brixCard);

    // Nút "Tùy chọn" sẽ xuất hiện
    await waitFor(() => {
      expect(screen.getByText('Tùy chọn')).toBeInTheDocument();
    });

    // Bấm đổi sang "Bắt buộc"
    const toggleBtn = screen.getByText('Tùy chọn');
    fireEvent.click(toggleBtn);

    await waitFor(() => {
      expect(screen.getByText('Bắt buộc')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC04: SUBMIT ĐỒNG BỘ MA TRẬN GỌI API SYNC
  it('TC04 - Submit đồng bộ ma trận gọi đúng API sync', async () => {
    render(
      <MemoryRouter initialEntries={['/category-attributes/create']}>
        <Routes>
          <Route path="/category-attributes/create" element={<CategoryAttributeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('-- Chọn danh mục sản phẩm để cấu hình --')).toBeInTheDocument();
    });

    // Mở dropdown chọn danh mục
    const selectTrigger = screen.getByText('-- Chọn danh mục sản phẩm để cấu hình --');
    fireEvent.click(selectTrigger);

    await waitFor(() => {
      expect(screen.getByText('Bơ Sáp & Sầu Riêng (Trái Cây Đặc Sản)')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Bơ Sáp & Sầu Riêng (Trái Cây Đặc Sản)'));

    await waitFor(() => {
      expect(screen.getByText('Độ ngọt (Brix)')).toBeInTheDocument();
    });

    // Chọn tất cả
    const selectAllBtn = screen.getByRole('button', { name: /Chọn tất cả/i });
    fireEvent.click(selectAllBtn);

    // Bấm Submit
    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(categoryAttributeApi.sync).toHaveBeenCalledWith(
        expect.objectContaining({
          categoryId: 1,
          attributes: expect.arrayContaining([
            expect.objectContaining({ attributeDefinitionId: 10 }),
            expect.objectContaining({ attributeDefinitionId: 11 }),
            expect.objectContaining({ attributeDefinitionId: 12 }),
          ]),
        })
      );
    });
  });
  // #endregion
});
