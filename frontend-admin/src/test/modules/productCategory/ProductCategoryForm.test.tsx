import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProductCategoryForm from '../../../pages/productCategory/ProductCategoryForm';
import { productCategoryApi } from '../../../api/productCategoryApi';
import { productCategoryGroupApi } from '../../../api/productCategoryGroupApi';

// Mock APIs
vi.mock('../../../api/productCategoryApi', () => ({
  productCategoryApi: {
    getAllList: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

vi.mock('../../../api/productCategoryGroupApi', () => ({
  productCategoryGroupApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 4: PRODUCT CATEGORY
 * COMPONENT TEST: ProductCategoryForm (Form Thêm / Sửa Danh Mục)
 * ============================================================================
 */
describe('Module 04 - ProductCategoryForm Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (productCategoryApi.getAllList as any).mockResolvedValue([
      { id: 1, code: 'VEG_ORGANIC', name: 'Rau xanh hữu cơ' },
    ]);
    (productCategoryGroupApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Thực phẩm tươi sống' },
    ]);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  it('TC01 - Render đầy đủ các trường trong form tạo mới danh mục sản phẩm', async () => {
    render(
      <MemoryRouter initialEntries={['/product-categories/create']}>
        <Routes>
          <Route path="/product-categories/create" element={<ProductCategoryForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Danh Mục Sản Phẩm')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('VD: DIENTHOAI')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('VD: Điện thoại di động')).toBeInTheDocument();
      expect(screen.getByText('Thuộc Nhóm Danh Mục')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('https://...')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: VALIDATION BẮT BUỘC KHI SUBMIT TRỐNG
  it('TC02 - Hiển thị lỗi validation khi submit dữ liệu trống', async () => {
    render(
      <MemoryRouter initialEntries={['/product-categories/create']}>
        <Routes>
          <Route path="/product-categories/create" element={<ProductCategoryForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Danh Mục Sản Phẩm')).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập mã danh mục.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập tên danh mục.')).toBeInTheDocument();
    });

    expect(productCategoryApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: CLIENT SIDE DUPLICATE CHECK
  it('TC03 - Báo lỗi trùng lặp mã danh mục ngay trên giao diện', async () => {
    render(
      <MemoryRouter initialEntries={['/product-categories/create']}>
        <Routes>
          <Route path="/product-categories/create" element={<ProductCategoryForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Danh Mục Sản Phẩm')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('VD: DIENTHOAI');
    const nameInput = screen.getByPlaceholderText('VD: Điện thoại di động');

    fireEvent.change(codeInput, { target: { value: 'veg_organic' } });
    fireEvent.change(nameInput, { target: { value: 'Rau cải sạch' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Mã danh mục này đã tồn tại!')).toBeInTheDocument();
    });

    expect(productCategoryApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC04: SUBMIT FORM TẠO MỚI THÀNH CÔNG
  it('TC04 - Submit form tạo mới danh mục thành công và gọi API create', async () => {
    (productCategoryApi.create as any).mockResolvedValue({ id: 2 });

    render(
      <MemoryRouter initialEntries={['/product-categories/create']}>
        <Routes>
          <Route path="/product-categories/create" element={<ProductCategoryForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Danh Mục Sản Phẩm')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('VD: DIENTHOAI');
    const nameInput = screen.getByPlaceholderText('VD: Điện thoại di động');

    fireEvent.change(codeInput, { target: { value: 'MEAT_FRESH' } });
    fireEvent.change(nameInput, { target: { value: 'Thịt tươi các loại' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(productCategoryApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          code: 'MEAT_FRESH',
          name: 'Thịt tươi các loại',
          isActive: true,
        })
      );
    });
  });
  // #endregion

  // #region TC05: EDIT MODE - LOAD DỮ LIỆU VÀ UPDATE THÀNH CÔNG
  it('TC05 - Load dữ liệu chi tiết ở chế độ Edit và submit cập nhật thành công', async () => {
    (productCategoryApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'VEG_ORGANIC',
      name: 'Rau xanh hữu cơ',
      categoryGroupId: 1,
      description: 'Mô tả rau xanh',
      imagePath: 'https://example.com/veg.png',
      isActive: true,
    });
    (productCategoryApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/product-categories/edit/1']}>
        <Routes>
          <Route path="/product-categories/edit/:id" element={<ProductCategoryForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Danh Mục Sản Phẩm')).toBeInTheDocument();
      expect(screen.getByDisplayValue('VEG_ORGANIC')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Rau xanh hữu cơ')).toBeInTheDocument();
    });

    const nameInput = screen.getByDisplayValue('Rau xanh hữu cơ');
    fireEvent.change(nameInput, { target: { value: 'Rau xanh chuẩn VietGAP' } });

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(productCategoryApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          code: 'VEG_ORGANIC',
          name: 'Rau xanh chuẩn VietGAP',
        })
      );
    });
  });
  // #endregion
});
