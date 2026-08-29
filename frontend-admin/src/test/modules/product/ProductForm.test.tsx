import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProductForm from '../../../pages/product/ProductForm';
import { productApi } from '../../../api/productApi';
import { productCategoryApi } from '../../../api/productCategoryApi';
import { uomApi } from '../../../api/uomApi';

// Mock APIs
vi.mock('../../../api/productApi', () => ({
  productApi: {
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

vi.mock('../../../api/uomApi', () => ({
  uomApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 5: PRODUCT & PRICING
 * 🧪 COMPONENT TEST: ProductForm (Form Thêm / Sửa Sản Phẩm Gốc)
 * ============================================================================
 */
describe('Module 05 - ProductForm Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (productCategoryApi.getAllList as any).mockResolvedValue([
      { id: 10, name: 'Rau củ quả' },
    ]);
    (uomApi.getAllList as any).mockResolvedValue([
      { id: 1, name: 'Kilogram', code: 'KG' },
    ]);
    (productApi.getAllList as any).mockResolvedValue([
      { id: 1, code: 'PROD-EXISTING', name: 'Sản phẩm cũ' },
    ]);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  it('TC01 - Render đầy đủ các trường trong form tạo mới sản phẩm', async () => {
    render(
      <MemoryRouter initialEntries={['/products/create']}>
        <Routes>
          <Route path="/products/create" element={<ProductForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Sản Phẩm')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('VD: IPHONE-15')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('VD: Apple iPhone 15 Pro Max')).toBeInTheDocument();
      expect(screen.getByText('Danh mục sản phẩm')).toBeInTheDocument();
      expect(screen.getByText('Đơn vị tính cơ bản')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: VALIDATION KHI SUBMIT TRỐNG
  it('TC02 - Hiển thị lỗi validation khi submit thiếu mã, tên hoặc đơn vị tính', async () => {
    render(
      <MemoryRouter initialEntries={['/products/create']}>
        <Routes>
          <Route path="/products/create" element={<ProductForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Sản Phẩm')).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập mã sản phẩm.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập tên sản phẩm.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng chọn đơn vị tính cơ bản.')).toBeInTheDocument();
    });

    expect(productApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: CLIENT SIDE DUPLICATE CODE CHECK
  it('TC03 - Báo lỗi trùng lặp mã sản phẩm ngay trên form', async () => {
    render(
      <MemoryRouter initialEntries={['/products/create']}>
        <Routes>
          <Route path="/products/create" element={<ProductForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Sản Phẩm')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('VD: IPHONE-15');
    const nameInput = screen.getByPlaceholderText('VD: Apple iPhone 15 Pro Max');

    fireEvent.change(codeInput, { target: { value: 'prod-existing' } });
    fireEvent.change(nameInput, { target: { value: 'Sản phẩm mới' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Mã sản phẩm này đã tồn tại!')).toBeInTheDocument();
    });

    expect(productApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC04: SUBMIT FORM TẠO MỚI THÀNH CÔNG
  it('TC04 - Submit form tạo mới thành công và chuẩn hóa mã sản phẩm', async () => {
    (productApi.create as any).mockResolvedValue({ id: 5 });

    render(
      <MemoryRouter initialEntries={['/products/create']}>
        <Routes>
          <Route path="/products/create" element={<ProductForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Sản Phẩm')).toBeInTheDocument();
      expect(screen.getByText('VD: Cái, Hộp, Chiếc...')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('VD: IPHONE-15');
    const nameInput = screen.getByPlaceholderText('VD: Apple iPhone 15 Pro Max');

    fireEvent.change(codeInput, { target: { value: 'prod-dualeo' } });
    fireEvent.change(nameInput, { target: { value: 'Dưa leo baby Đà Lạt' } });

    // Chọn UoM ID = 1 qua UI custom FormSelect
    const uomSelect = screen.getByText('-- Chọn đơn vị tính --');
    fireEvent.click(uomSelect);
    const option = await screen.findByText('Kilogram (KG)');
    fireEvent.click(option);

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(productApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          code: 'PROD-DUALEO',
          name: 'Dưa leo baby Đà Lạt',
          baseUoMId: 1,
          isActive: true,
        })
      );
    });
  });
  // #endregion

  // #region TC05: EDIT MODE - LOAD CHI TIẾT VÀ UPDATE
  it('TC05 - Load dữ liệu chi tiết ở chế độ Edit và submit cập nhật thành công', async () => {
    (productApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'PROD-XOAI',
      name: 'Xoài Cát Chu',
      description: 'Xoài loại 1',
      imagePath: 'https://example.com/xoai.png',
      categoryId: 10,
      baseUoMId: 1,
      isActive: true,
    });
    (productApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/products/edit/1']}>
        <Routes>
          <Route path="/products/edit/:id" element={<ProductForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Sản Phẩm Gốc')).toBeInTheDocument();
      expect(screen.getByDisplayValue('PROD-XOAI')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Xoài Cát Chu')).toBeInTheDocument();
    });

    const nameInput = screen.getByDisplayValue('Xoài Cát Chu');
    fireEvent.change(nameInput, { target: { value: 'Xoài Cát Chu Tiền Giang' } });

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(productApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          code: 'PROD-XOAI',
          name: 'Xoài Cát Chu Tiền Giang',
        })
      );
    });
  });
  // #endregion
});
