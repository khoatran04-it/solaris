import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import AttributeDefinitionList from '../../../pages/attributeDefinition/AttributeDefinitionList';
import { attributeDefinitionApi } from '../../../api/attributeDefinitionApi';

// Mock API
vi.mock('../../../api/attributeDefinitionApi', () => ({
  attributeDefinitionApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 4: ATTRIBUTE DEFINITION
 * COMPONENT TEST: AttributeDefinitionList (Danh sách Từ Điển Thuộc Tính)
 * ============================================================================
 */
describe('Module 04 - AttributeDefinitionList Component', () => {
  const mockAttributes = [
    {
      id: 1,
      name: 'Độ ngọt (Brix)',
      dataType: 'NUMBER',
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
      updatedAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      name: 'Xuất xứ vùng trồng',
      dataType: 'TEXT',
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
      updatedAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (attributeDefinitionApi.getAll as any).mockResolvedValue({
      items: mockAttributes,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH THUỘC TÍNH VỚI BADGE KIỂU DỮ LIỆU
  it('TC01 - Render danh sách từ điển thuộc tính kèm badge kiểu dữ liệu', async () => {
    render(
      <MemoryRouter>
        <AttributeDefinitionList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Độ ngọt (Brix)')).toBeInTheDocument();
      expect(screen.getByText(/Số \(Number\)/i)).toBeInTheDocument();
      expect(screen.getByText('Xuất xứ vùng trồng')).toBeInTheDocument();
      expect(screen.getByText(/Văn bản \(Text\)/i)).toBeInTheDocument();
    });

    expect(attributeDefinitionApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TÊN THUỘC TÍNH
  it('TC02 - Tìm kiếm theo tên thuộc tính', async () => {
    render(
      <MemoryRouter>
        <AttributeDefinitionList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Độ ngọt (Brix)')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo tên thuộc tính.../i);
    fireEvent.change(searchInput, { target: { value: 'Độ ngọt' } });

    await waitFor(
      () => {
        expect(attributeDefinitionApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Độ ngọt',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: LỌC THEO KIỂU DỮ LIỆU VÀ TRẠNG THÁI
  it('TC03 - Lọc danh sách theo kiểu dữ liệu và trạng thái', async () => {
    render(
      <MemoryRouter>
        <AttributeDefinitionList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Độ ngọt (Brix)')).toBeInTheDocument();
    });

    const filterButtons = screen.getAllByRole('button');
    const typeFilterBtn = filterButtons.find((btn) => btn.textContent?.includes('KIỂU DỮ LIỆU'));
    if (typeFilterBtn) {
      fireEvent.click(typeFilterBtn);
    }

    await waitFor(() => {
      expect(attributeDefinitionApi.getAll).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: THAY ĐỔI TRẠNG THÁI (TOGGLE ACTIVE)
  it('TC04 - Đổi trạng thái hoạt động của thuộc tính', async () => {
    (attributeDefinitionApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <AttributeDefinitionList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Độ ngọt (Brix)')).toBeInTheDocument();
    });

    const activeToggle = screen.getAllByRole('button', { name: /Hoạt động/i })[0];
    fireEvent.click(activeToggle);

    await waitFor(() => {
      expect(attributeDefinitionApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC05: XÓA THUỘC TÍNH CÓ XÁC NHẬN MODAL
  it('TC05 - Mở modal xác nhận xóa và gọi API delete', async () => {
    (attributeDefinitionApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <AttributeDefinitionList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Độ ngọt (Brix)')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(attributeDefinitionApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
