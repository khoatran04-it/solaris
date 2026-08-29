import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Megaphone, Edit3, Trash2, Tag } from 'lucide-react';

// API & Types
import { promotionCampaignApi } from '../../api/promotionCampaignApi';
import { PromotionCampaign } from '../../types/promotionCampaign';

// Commons Components
import { Toast } from '../../components/commons/Toast';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';
import { CustomFilter } from '../../components/commons/CustomFilter';
import { CustomDateFilter } from '../../components/commons/CustomDateFilter';

// Atomic Components
import {
  ListPageContainer,
  ListHeader,
  ListCard,
  TableLoading,
  TableEmpty,
  ListPagination,
  DateTimeCell,
} from '../../components/commons/ListUI';

const PromotionCampaignList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
  const [data, setData] = useState<PromotionCampaign[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const pageSize = 10;

  // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
  const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
  const [startDateFilter, setStartDateFilter] = useState<Date | null>(null);
  const [endDateFilter, setEndDateFilter] = useState<Date | null>(null);

  // --- OPTIONS CHO BỘ LỌC ---
  const statusOptions = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 },
  ];

  // --- STATE MODAL & TOAST ---
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deletingRecord, setDeletingRecord] = useState<PromotionCampaign | null>(null);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'error' | 'warning';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- EFFECT: Debounce Search ---
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // --- EFFECT: Reset trang khi bộ lọc thay đổi ---
  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, statusFilter, startDateFilter, endDateFilter]);

  // --- EFFECT: Fetch Data ---
  const fetchData = async () => {
    setIsLoading(true);
    try {
      const response = await promotionCampaignApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        isActive: statusFilter.length === 1 ? statusFilter[0] === 1 : undefined,
        startDate: startDateFilter ? startDateFilter.toLocaleDateString('en-CA') : undefined,
        endDate: endDateFilter ? endDateFilter.toLocaleDateString('en-CA') : undefined,
      });

      setData(response.items || []);
      setTotalPages(response.totalPages || 0);
      setTotalItems(response.totalRecords || 0);
    } catch (error) {
      showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DỮ LIỆU CHIẾN DỊCH');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentPage, debouncedSearch, statusFilter, startDateFilter, endDateFilter]);

  // --- HANDLERS ---
  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const confirmDelete = async () => {
    if (!deletingRecord) return;
    try {
      await promotionCampaignApi.delete(deletingRecord.id);
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Xóa thành công chiến dịch "${deletingRecord.name}"`);
    } catch (error: any) {
      showToast('error', error?.response?.data?.message || 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  const handleToggleActive = async (id: number, currentStatus: boolean) => {
    try {
      await promotionCampaignApi.toggleActive(id);
      fetchData();
      showToast('success', `Đã ${currentStatus ? 'tạm khóa' : 'kích hoạt'} chiến dịch`);
    } catch (error) {
      showToast('error', 'Không thể thay đổi trạng thái');
    }
  };

  // --- HELPER FUNC: Lấy màu và nhãn cho thời hạn chiến dịch ---
  const getCampaignTimeStatus = (startStr: string, endStr: string, isActive: boolean) => {
    if (!isActive)
      return { text: 'Đã khóa', style: 'bg-slate-100 text-slate-500 border-slate-200' };

    const now = new Date().getTime();
    const start = new Date(startStr).getTime();
    const end = new Date(endStr).getTime();

    if (now < start)
      return { text: 'Sắp diễn ra', style: 'bg-amber-50 text-amber-600 border-amber-200/60' };
    if (now > end)
      return {
        text: 'Đã kết thúc',
        style: 'bg-slate-50 text-slate-400 border-slate-200 border-dashed',
      };

    return {
      text: 'Đang diễn ra',
      style: 'bg-emerald-50 text-emerald-700 border-emerald-200 animate-pulse-slow',
    };
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Chiến Dịch Khuyến Mãi"
        subtitle="Quản lý các đợt giảm giá, xả kho và chương trình ưu đãi sản phẩm"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/promotions/create')}
        icon={Megaphone}
        searchPlaceholder="Tìm kiếm theo tên chiến dịch..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[30%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Thông Tin Chiến Dịch
                </th>

                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  Mức Giảm Giá
                </th>

                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomFilter
                      title="TRẠNG THÁI (HỆ THỐNG)"
                      options={statusOptions}
                      selectedValues={statusFilter}
                      onApply={setStatusFilter}
                    />
                  </div>
                </th>

                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="TỪ NGÀY"
                      selectedDate={startDateFilter}
                      onApply={setStartDateFilter}
                    />
                  </div>
                </th>

                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="ĐẾN NGÀY"
                      selectedDate={endDateFilter}
                      onApply={setEndDateFilter}
                    />
                  </div>
                </th>

                <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  Thao Tác
                </th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <TableLoading colSpan={6} />
              ) : data.length > 0 ? (
                data.map((item) => {
                  const timeStatus = getCampaignTimeStatus(
                    item.startDate,
                    item.endDate,
                    item.isActive
                  );

                  return (
                    <tr
                      key={item.id}
                      className="hover:bg-slate-50/80 transition-colors duration-200 group"
                    >
                      {/* CELL 1: THÔNG TIN CHIẾN DỊCH */}
                      <td className="py-3 px-6">
                        <div className="flex items-start gap-3.5">
                          <div className="w-10 h-10 rounded-xl bg-orange-50 border border-orange-100 flex items-center justify-center shrink-0">
                            <Tag size={20} className="text-orange-500" />
                          </div>
                          <div className="flex flex-col">
                            <span className="font-extrabold text-slate-800 text-[14px] leading-tight">
                              {item.name}
                            </span>
                            <div className="flex items-center gap-2 mt-1.5">
                              <span
                                className={`inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold border uppercase tracking-wide ${timeStatus.style}`}
                              >
                                {timeStatus.text}
                              </span>
                              <span className="text-[11px] text-slate-500 line-clamp-1">
                                {item.description}
                              </span>
                            </div>
                          </div>
                        </div>
                      </td>

                      {/* CELL 2: MỨC GIẢM */}
                      <td className="py-3 px-2">
                        <span className="inline-flex items-center px-2.5 py-1 rounded-lg text-sm font-bold bg-rose-50 text-rose-600 border border-rose-200/60 shadow-sm">
                          {item.isPercentage
                            ? `Giảm ${item.discountValue}%`
                            : `-${item.discountValue.toLocaleString('vi-VN')} ₫`}
                        </span>
                      </td>

                      {/* CELL 3: TRẠNG THÁI HỆ THỐNG */}
                      <td className="py-3 px-2 text-center">
                        <div className="flex justify-center">
                          <button
                            onClick={() => handleToggleActive(item.id, item.isActive)}
                            className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold transition-all cursor-pointer ${
                              item.isActive
                                ? 'bg-emerald-50 text-emerald-700 border border-emerald-200 hover:bg-emerald-100'
                                : 'bg-slate-100 text-slate-600 border border-slate-200 hover:bg-slate-200'
                            }`}
                            title="Nhấn để đổi trạng thái"
                          >
                            <span
                              className={`w-1.5 h-1.5 rounded-full ${item.isActive ? 'bg-emerald-500' : 'bg-slate-400'}`}
                            ></span>
                            {item.isActive ? 'Hoạt động' : 'Tạm khóa'}
                          </button>
                        </div>
                      </td>

                      {/* CELL 4 & 5 (START - END DATE) */}
                      <td className="py-3 px-2 text-center">
                        <DateTimeCell isoString={item.startDate} />
                      </td>
                      <td className="py-3 px-2 text-center">
                        <DateTimeCell isoString={item.endDate} />
                      </td>

                      {/* CELL 6: ACTIONS */}
                      <td className="py-3 px-6">
                        <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                          <button
                            onClick={() => navigate(`/promotions/edit/${item.id}`)}
                            className="p-1.5 text-slate-400 hover:text-yellow-600 hover:bg-yellow-50 rounded-lg transition-colors"
                            title="Sửa thông tin & Gắn sản phẩm"
                          >
                            <Edit3 size={17} strokeWidth={2.5} />
                          </button>
                          <button
                            onClick={() => {
                              setDeletingRecord(item);
                              setIsModalOpen(true);
                            }}
                            className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                            title="Xóa"
                          >
                            <Trash2 size={17} strokeWidth={2.5} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              ) : (
                <TableEmpty colSpan={6} message="Chưa có chiến dịch khuyến mãi nào." />
              )}
            </tbody>
          </table>
        </div>

        <ListPagination
          currentPage={currentPage}
          totalPages={totalPages}
          totalItems={totalItems}
          onPageChange={setCurrentPage}
          isLoading={isLoading}
        />
      </ListCard>

      <ConfirmDeleteModal
        isOpen={isModalOpen}
        itemName={deletingRecord?.name || ''}
        onClose={() => setIsModalOpen(false)}
        onConfirm={confirmDelete}
      />
    </ListPageContainer>
  );
};

export default PromotionCampaignList;
