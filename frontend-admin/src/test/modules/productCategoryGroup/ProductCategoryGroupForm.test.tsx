import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProductCategoryGroupForm from '../../../pages/productCategoryGroup/ProductCategoryGroupForm';
import { productCategoryGroupApi } from '../../../api/productCategoryGroupApi';

// Mock API
vi.mock('../../../api/productCategoryGroupApi', () => ({
  productCategoryGroupApi: {
    getAllList: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 4: PRODUCT CATEGORY GROUP
 * 🧪 COMPONENT TEST: ProductCategoryGroupForm (Form Thêm / Sửa Nhóm Ngành Hàng)
 * ============================================================================
 */
describe('Module 04 - ProductCategoryGroupForm Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (productCategoryGroupApi.getAllList as any).mockResolvedValue([
      { id: 1, code: 'FRESH_FOOD', name: 'Thực phẩm tươi sống' },
    ]);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  it('TC01 - Render đầy đủ các trường trong form tạo mới nhóm ngành hàng', async () => {
    render(
      <MemoryRouter initialEntries={['/product-category-groups/create']}>
        <Routes>
          <Route path="/product-category-groups/create" element={<ProductCategoryGroupForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Thêm Mới Nhóm Ngành Hàng')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: FRESH_PRODUCE')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: Nông Sản Tươi Sống')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nhập chi tiết mô tả...')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('https://...')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION BẮT BUỘC KHI SUBMIT TRỐNG
  it('TC02 - Hiển thị lỗi validation khi submit dữ liệu trống', async () => {
    render(
      <MemoryRouter initialEntries={['/product-category-groups/create']}>
        <Routes>
          <Route path="/product-category-groups/create" element={<ProductCategoryGroupForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Nhóm Ngành Hàng')).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập mã nhóm ngành hàng.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập tên nhóm ngành hàng.')).toBeInTheDocument();
    });

    expect(productCategoryGroupApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: CLIENT SIDE DUPLICATE CHECK
  it('TC03 - Báo lỗi trùng lặp mã nhóm ngay trên giao diện', async () => {
    render(
      <MemoryRouter initialEntries={['/product-category-groups/create']}>
        <Routes>
          <Route path="/product-category-groups/create" element={<ProductCategoryGroupForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Nhóm Ngành Hàng')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('VD: FRESH_PRODUCE');
    const nameInput = screen.getByPlaceholderText('VD: Nông Sản Tươi Sống');

    // Nhập trùng mã FRESH_FOOD (đã có trong danh sách getAllList)
    fireEvent.change(codeInput, { target: { value: 'fresh_food' } });
    fireEvent.change(nameInput, { target: { value: 'Thực phẩm sạch' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Mã nhóm này đã tồn tại!')).toBeInTheDocument();
    });

    expect(productCategoryGroupApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC04: SUBMIT FORM TẠO MỚI THÀNH CÔNG
  it('TC04 - Submit form tạo mới thành công và gọi API create', async () => {
    (productCategoryGroupApi.create as any).mockResolvedValue({ id: 2 });

    render(
      <MemoryRouter initialEntries={['/product-category-groups/create']}>
        <Routes>
          <Route path="/product-category-groups/create" element={<ProductCategoryGroupForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Nhóm Ngành Hàng')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('VD: FRESH_PRODUCE');
    const nameInput = screen.getByPlaceholderText('VD: Nông Sản Tươi Sống');

    fireEvent.change(codeInput, { target: { value: 'BEVERAGE' } });
    fireEvent.change(nameInput, { target: { value: 'Đồ uống & Giải khát' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(productCategoryGroupApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          code: 'BEVERAGE',
          name: 'Đồ uống & Giải khát',
          isActive: true,
        })
      );
    });
  });
  // #endregion

  // #region TC05: EDIT MODE - LOAD DỮ LIỆU VÀ UPDATE THÀNH CÔNG
  it('TC05 - Load dữ liệu chi tiết ở chế độ Edit và submit cập nhật thành công', async () => {
    (productCategoryGroupApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'FRESH_FOOD',
      name: 'Thực phẩm tươi sống',
      description: 'Mô tả ban đầu',
      imagePath: 'https://example.com/food.png',
      isActive: true,
    });
    (productCategoryGroupApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/product-category-groups/edit/1']}>
        <Routes>
          <Route path="/product-category-groups/edit/:id" element={<ProductCategoryGroupForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Nhóm Ngành Hàng')).toBeInTheDocument();
      expect(screen.getByDisplayValue('FRESH_FOOD')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Thực phẩm tươi sống')).toBeInTheDocument();
    });

    const nameInput = screen.getByDisplayValue('Thực phẩm tươi sống');
    fireEvent.change(nameInput, { target: { value: 'Thực phẩm tươi sạch cao cấp' } });

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(productCategoryGroupApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          code: 'FRESH_FOOD',
          name: 'Thực phẩm tươi sạch cao cấp',
        })
      );
    });
  });
  // #endregion
});
