import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { ModalSupplierProduct } from '../../../components/modals/ModalSupplierProduct';
import { supplierApi } from '../../../api/supplierApi';
import { productVariantApi } from '../../../api/productVariantApi';
import { productApi } from '../../../api/productApi';
import { uomApi } from '../../../api/uomApi';

vi.mock('../../../api/supplierApi', () => ({
  supplierApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/productVariantApi', () => ({
  productVariantApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/productApi', () => ({
  productApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/uomApi', () => ({
  uomApi: {
    getAllList: vi.fn(),
  },
}));

describe('ModalSupplierProduct Component', () => {
  const mockSuppliers = [{ id: 1, code: 'NCC01', name: 'Nông trại Xanh Đà Lạt' }];

  const mockVariants = [
    {
      id: 10,
      code: 'SKU-BO-034',
      name: 'Bơ Sáp 034 Đắc Lắc',
      productId: 100,
      baseUoMId: 5,
      baseUoMName: 'Kilogram',
      prices: [{ priceId: 1, uoMId: 5, uoMName: 'Kilogram', price: 45000, isDefault: true }],
    },
    {
      id: 11,
      code: 'SKU-MI-GOM',
      name: 'Mì Gói Hảo Hảo',
      productId: 101,
      baseUoMId: 8,
      baseUoMName: 'Gói',
      prices: [{ priceId: 2, uoMId: 8, uoMName: 'Gói', price: 4000, isDefault: true }],
    },
  ];

  const mockProducts = [
    { id: 100, name: 'Bơ Sáp 034', baseUoMId: 5, baseUoMName: 'Kilogram' },
    { id: 101, name: 'Mì Ăn Liền Hảo Hảo', baseUoMId: 8, baseUoMName: 'Gói' },
  ];

  const mockUoMs = [
    { id: 5, name: 'Kilogram', code: 'KG' },
    { id: 8, name: 'Gói', code: 'GOI' },
    { id: 9, name: 'Thùng', code: 'THUNG' },
  ];

  const mockOnSave = vi.fn();
  const mockOnClose = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    (supplierApi.getAllList as any).mockResolvedValue(mockSuppliers);
    (productVariantApi.getAllList as any).mockResolvedValue(mockVariants);
    (productApi.getAllList as any).mockResolvedValue(mockProducts);
    (uomApi.getAllList as any).mockResolvedValue(mockUoMs);
  });

  it('TC01 - Render modal đúng tiêu đề và tải đầy đủ danh mục khi isOpen = true', async () => {
    render(<ModalSupplierProduct isOpen={true} onClose={mockOnClose} onSave={mockOnSave} />);

    expect(screen.getByText('Thêm Sản Phẩm Vào Danh Mục NCC')).toBeInTheDocument();
    expect(screen.getByText(/Thiết lập đơn giá nhập và chính sách cung ứng/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(supplierApi.getAllList).toHaveBeenCalled();
      expect(productVariantApi.getAllList).toHaveBeenCalled();
      expect(uomApi.getAllList).toHaveBeenCalled();
      expect(productApi.getAllList).toHaveBeenCalled();
    });
  });

  it('TC02 - Tự động nhận diện ĐVT và khóa cứng Đơn vị tính mua hàng khi chọn Sản phẩm biến thể', async () => {
    render(<ModalSupplierProduct isOpen={true} onClose={mockOnClose} onSave={mockOnSave} />);

    await waitFor(() => {
      expect(screen.getByText('Chọn sản phẩm biến thể...')).toBeInTheDocument();
    });

    // Mở dropdown chọn biến thể
    const selectVariantTrigger = screen.getByText('Chọn sản phẩm biến thể...');
    fireEvent.click(selectVariantTrigger);

    // Click chọn biến thể 'SKU-BO-034 - Bơ Sáp 034 Đắc Lắc'
    await waitFor(() => {
      expect(screen.getByText('SKU-BO-034 - Bơ Sáp 034 Đắc Lắc')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('SKU-BO-034 - Bơ Sáp 034 Đắc Lắc'));

    // Kiểm tra: Thẻ Preview biến thể xuất hiện
    await waitFor(() => {
      expect(screen.getByText('SKU-BO-034')).toBeInTheDocument();
      expect(screen.getByText('Bơ Sáp 034 Đắc Lắc')).toBeInTheDocument();
    });

    // Kiểm tra: Đơn vị tính tự động điền 'Kilogram' và khóa cứng kèm thông báo badge 🔒
    expect(screen.getByText(/Cố định theo đơn vị tính của sản phẩm/i)).toBeInTheDocument();
    expect(screen.getByText('Kilogram (ĐVT của sản phẩm)')).toBeInTheDocument();
  });

  it('TC03 - Kiểm tra validation khi form thiếu dữ liệu bắt buộc', async () => {
    render(<ModalSupplierProduct isOpen={true} onClose={mockOnClose} onSave={mockOnSave} />);

    await waitFor(() => {
      expect(screen.getByText('Thêm Vào Bảng Giá')).toBeInTheDocument();
    });

    // Submit form khi chưa nhập gì
    const submitBtn = screen.getByText('Thêm Vào Bảng Giá');
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng chọn nhà cung cấp.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng chọn sản phẩm biến thể.')).toBeInTheDocument();
    });

    expect(mockOnSave).not.toHaveBeenCalled();
  });

  it('TC04 - Submit thành công dữ liệu bảng giá với ĐVT đã được khóa cố định', async () => {
    render(<ModalSupplierProduct isOpen={true} onClose={mockOnClose} onSave={mockOnSave} />);

    await waitFor(() => {
      expect(screen.getByText('Chọn nhà cung cấp...')).toBeInTheDocument();
    });

    // Chọn nhà cung cấp
    fireEvent.click(screen.getByText('Chọn nhà cung cấp...'));
    await waitFor(() => {
      expect(screen.getByText('NCC01 - Nông trại Xanh Đà Lạt')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('NCC01 - Nông trại Xanh Đà Lạt'));

    // Chọn biến thể
    fireEvent.click(screen.getByText('Chọn sản phẩm biến thể...'));
    await waitFor(() => {
      expect(screen.getByText('SKU-MI-GOM - Mì Gói Hảo Hảo')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('SKU-MI-GOM - Mì Gói Hảo Hảo'));

    // Điền Đơn giá nhập và MOQ
    const priceInput = screen.getByPlaceholderText('VD: 50.000');
    fireEvent.change(priceInput, { target: { value: '3800' } });

    const moqInput = screen.getByPlaceholderText('VD: 10');
    fireEvent.change(moqInput, { target: { value: '100' } });

    // Submit
    const submitBtn = screen.getByText('Thêm Vào Bảng Giá');
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(mockOnSave).toHaveBeenCalledWith({
        supplierId: 1,
        variantId: 11,
        purchaseUoMId: 8, // Tự động nhận diện ĐVT Gói
        lastImportPrice: 3800,
        minimumOrderQuantity: 100,
        leadTimeDays: 0,
        supplierSKU: undefined,
        isActive: true,
      });
      expect(mockOnClose).toHaveBeenCalled();
    });
  });
});
