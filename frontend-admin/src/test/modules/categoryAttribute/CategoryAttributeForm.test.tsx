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
 * 📦 MODULE 4: CATEGORY ATTRIBUTE
 * 🧪 COMPONENT TEST: CategoryAttributeForm (Form Thêm / Sửa Cấu Hình Thuộc Tính)
 * ============================================================================
 */
describe('Module 04 - CategoryAttributeForm Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (productCategoryApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Rau củ quả sạch' },
    ]);
    (attributeDefinitionApi.getAllList as any).mockResolvedValue([
      { id: 10, name: 'Độ ngọt (Brix)' },
      { id: 11, name: 'Xuất xứ' },
    ]);
  });

  // #region TC01: RENDER FORM GÁN MỚI
  it('TC01 - Render đầy đủ các trường trong form gán thuộc tính vào danh mục', async () => {
    render(
      <MemoryRouter initialEntries={['/category-attributes/create']}>
        <Routes>
          <Route path="/category-attributes/create" element={<CategoryAttributeForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Gắn Thuộc Tính Mới')).toBeInTheDocument();
    expect(screen.getByText('-- Chọn danh mục --')).toBeInTheDocument();
    expect(screen.getByText('-- Chọn thuộc tính --')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION BẮT BUỘC KHI CHƯA CHỌN DANH MỤC VÀ THUỘC TÍNH
  it('TC02 - Hiển thị lỗi validation khi submit khi chưa chọn danh mục hoặc thuộc tính', async () => {
    render(
      <MemoryRouter initialEntries={['/category-attributes/create']}>
        <Routes>
          <Route path="/category-attributes/create" element={<CategoryAttributeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Gắn Thuộc Tính Mới')).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng chọn danh mục sản phẩm.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng chọn thuộc tính cần gắn.')).toBeInTheDocument();
    });

    expect(categoryAttributeApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: XỬ LÝ LỖI TRÙNG LẶP TỪ BACKEND
  it('TC03 - Hiển thị thông báo lỗi khi Backend trả về lỗi trùng lặp cấu hình', async () => {
    (categoryAttributeApi.create as any).mockRejectedValue({
      response: {
        data: {
          message: 'Thuộc tính này đã được thiết lập cho danh mục được chọn.',
        },
      },
    });

    render(
      <MemoryRouter initialEntries={['/category-attributes/create']}>
        <Routes>
          <Route path="/category-attributes/create" element={<CategoryAttributeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Gắn Thuộc Tính Mới')).toBeInTheDocument();
    });

    // Chọn danh mục ID = 1
    const categorySelectBtn = screen.getByText('-- Chọn danh mục --');
    fireEvent.click(categorySelectBtn);
    const categoryOption = await screen.findByText('Rau củ quả sạch');
    fireEvent.click(categoryOption);

    // Chọn thuộc tính ID = 10
    const attributeSelectBtn = screen.getByText('-- Chọn thuộc tính --');
    fireEvent.click(attributeSelectBtn);
    const attributeOption = await screen.findByText('Độ ngọt (Brix)');
    fireEvent.click(attributeOption);

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Thuộc tính này đã được thiết lập cho danh mục được chọn.')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC04: SUBMIT GÁN THUỘC TÍNH MỚI THÀNH CÔNG
  it('TC04 - Submit gán thuộc tính mới thành công và gọi API create', async () => {
    (categoryAttributeApi.create as any).mockResolvedValue({ id: 2 });

    render(
      <MemoryRouter initialEntries={['/category-attributes/create']}>
        <Routes>
          <Route path="/category-attributes/create" element={<CategoryAttributeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Gắn Thuộc Tính Mới')).toBeInTheDocument();
    });

    // Chọn danh mục ID = 1
    const categorySelectBtn = screen.getByText('-- Chọn danh mục --');
    fireEvent.click(categorySelectBtn);
    const categoryOption = await screen.findByText('Rau củ quả sạch');
    fireEvent.click(categoryOption);

    // Chọn thuộc tính ID = 11 (Xuất xứ)
    const attributeSelectBtn = screen.getByText('-- Chọn thuộc tính --');
    fireEvent.click(attributeSelectBtn);
    const attributeOption = await screen.findByText('Xuất xứ');
    fireEvent.click(attributeOption);

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(categoryAttributeApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          categoryId: 1,
          attributeDefinitionId: 11,
          isRequired: false,
        })
      );
    });
  });
  // #endregion

  // #region TC05: EDIT MODE - LOAD DỮ LIỆU VÀ UPDATE THÀNH CÔNG
  it('TC05 - Load dữ liệu chi tiết ở chế độ Edit và submit cập nhật thành công', async () => {
    (categoryAttributeApi.getById as any).mockResolvedValue({
      id: 1,
      categoryId: 1,
      attributeDefinitionId: 10,
      isRequired: false,
    });
    (categoryAttributeApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/category-attributes/edit/1']}>
        <Routes>
          <Route path="/category-attributes/edit/:id" element={<CategoryAttributeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Cấu Hình')).toBeInTheDocument();
      expect(screen.getByText('Rau củ quả sạch')).toBeInTheDocument();
      expect(screen.getByText('Độ ngọt (Brix)')).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(categoryAttributeApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          categoryId: 1,
          attributeDefinitionId: 10,
        })
      );
    });
  });
  // #endregion
});
