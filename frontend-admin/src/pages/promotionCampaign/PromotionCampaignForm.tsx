import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Megaphone,
  Save,
  Plus,
  CheckSquare,
  Square,
  Search,
  ChevronRight,
  Box,
  Info,
  Package,
} from 'lucide-react';

// API & Types
import { promotionCampaignApi } from '../../api/promotionCampaignApi';
import { productVariantApi } from '../../api/productVariantApi';
import { PromotionCampaignPayload } from '../../types/promotionCampaign';
import { ProductVariant } from '../../types/productVariant';

// Components
import { Toast } from '../../components/commons/Toast';
import {
  PageContainer,
  FormCard,
  FormInput,
  FormHeader,
  FormSection,
  FormSelect,
  FormTextarea,
  SubmitButton,
} from '../../components/commons/FormUI';
import CustomDatePicker from '../../components/commons/CustomDatePicker';
import { TabGroup, TabButton } from '../../components/commons/TabUI';

type TabType = 'info' | 'products';

const INITIAL_STATE: PromotionCampaignPayload = {
  name: '',
  description: '',
  isPercentage: true,
  discountValue: 0,
  startDate: new Date().toISOString(),
  endDate: new Date(new Date().setDate(new Date().getDate() + 7)).toISOString(),
  isActive: true,
  variantIds: [],
};

const DISCOUNT_TYPE_OPTIONS = [
  { label: 'Phần trăm (%)', value: 1 },
  { label: 'Số tiền (VNĐ)', value: 0 },
];

const PromotionCampaignForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);

  // --- STATES CƠ BẢN ---
  const [formData, setFormData] = useState<PromotionCampaignPayload>(INITIAL_STATE);
  const [loading, setLoading] = useState(false);

  // --- STATES UI (TABS & TÌM KIẾM) ---
  const [activeTab, setActiveTab] = useState<TabType>('info');
  const [variantSearch, setVariantSearch] = useState('');
  const [allVariants, setAllVariants] = useState<ProductVariant[]>([]);

  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'warning' | 'error';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- EFFECTS ---
  useEffect(() => {
    productVariantApi.getAllList().then((res) => setAllVariants(res));

    if (isEditMode && id) {
      promotionCampaignApi
        .getById(Number(id))
        .then((res) => {
          setFormData({
            name: res.name,
            description: res.description || '',
            isPercentage: res.isPercentage,
            discountValue: res.discountValue,
            startDate: res.startDate,
            endDate: res.endDate,
            isActive: res.isActive,
            variantIds: res.appliedVariants.map((v) => v.variantId),
          });
        })
        .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU CHIẾN DỊCH'));
    }
  }, [id, isEditMode]);

  // --- LOGIC LỌC & TÌM KIẾM SẢN PHẨM ---
  const filteredVariants = allVariants.filter(
    (v) =>
      v.name.toLowerCase().includes(variantSearch.toLowerCase()) ||
      v.code.toLowerCase().includes(variantSearch.toLowerCase())
  );

  const isAllFilteredSelected =
    filteredVariants.length > 0 &&
    filteredVariants.every((v) => formData.variantIds?.includes(v.id));

  // --- HANDLERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const handleFieldChange = (field: keyof PromotionCampaignPayload, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const toggleVariant = (variantId: number) => {
    setFormData((prev) => {
      const currentIds = prev.variantIds || [];
      const newIds = currentIds.includes(variantId)
        ? currentIds.filter((item) => item !== variantId)
        : [...currentIds, variantId];
      return { ...prev, variantIds: newIds };
    });
  };

  const handleSelectAllFiltered = () => {
    if (isAllFilteredSelected) {
      const filteredIds = filteredVariants.map((v) => v.id);
      setFormData((prev) => ({
        ...prev,
        variantIds: prev.variantIds?.filter((item) => !filteredIds.includes(item)) || [],
      }));
    } else {
      const newIds = new Set([
        ...(formData.variantIds || []),
        ...filteredVariants.map((v) => v.id),
      ]);
      setFormData((prev) => ({
        ...prev,
        variantIds: Array.from(newIds),
      }));
    }
  };

  // --- SUBMIT ---
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const trimmedName = formData.name.trim();
    if (!trimmedName) return showToast('warning', 'Vui lòng nhập tên chiến dịch');
    if (formData.discountValue <= 0) return showToast('warning', 'Mức giảm giá phải lớn hơn 0');
    if (formData.isPercentage && formData.discountValue > 100)
      return showToast('warning', 'Mức giảm theo phần trăm không được vượt quá 100%');
    if (new Date(formData.startDate) >= new Date(formData.endDate))
      return showToast('warning', 'Ngày bắt đầu phải trước ngày kết thúc');

    setLoading(true);
    try {
      const cleanPayload: PromotionCampaignPayload = {
        ...formData,
        name: trimmedName,
        description: formData.description?.trim() || null,
        variantIds: formData.variantIds || [],
      };

      if (isEditMode && id) {
        await promotionCampaignApi.update(Number(id), cleanPayload);
        await promotionCampaignApi.addVariants(Number(id), {
          variantIds: cleanPayload.variantIds || [],
        });
        showToast('success', 'CẬP NHẬT CHIẾN DỊCH THÀNH CÔNG');
      } else {
        await promotionCampaignApi.create(cleanPayload);
        showToast('success', 'TẠO CHIẾN DỊCH THÀNH CÔNG');
      }
      setTimeout(() => navigate('/promotions'), 1000);
    } catch (error: any) {
      showToast('error', error?.response?.data?.message || 'CÓ LỖI XẢY RA KHI LƯU');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />
      <FormHeader
        title={isEditMode ? 'Chỉnh Sửa Chiến Dịch' : 'Tạo Chiến Dịch Khuyến Mãi'}
        subtitle="Cấu hình thông tin khuyến mãi và áp dụng cho sản phẩm"
        onBack={() => navigate('/promotions')}
        icon={Megaphone}
      />

      {/* BỘ TABS */}
      <div className="mb-5 flex items-center justify-between">
        <TabGroup>
          <TabButton
            active={activeTab === 'info'}
            onClick={() => setActiveTab('info')}
            label="THÔNG TIN CƠ BẢN"
            icon={Info}
          />
          <TabButton
            active={activeTab === 'products'}
            onClick={() => setActiveTab('products')}
            label={`SẢN PHẨM ÁP DỤNG (${formData.variantIds?.length || 0})`}
            icon={Package}
          />
        </TabGroup>
      </div>

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-6">
          {/* ================= TAB 1: THÔNG TIN CƠ BẢN ================= */}
          {activeTab === 'info' && (
            <div className="animate-in fade-in slide-in-from-bottom-4 duration-300">
              <FormSection title="Thiết Lập Chiến Dịch">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <FormInput
                    label="Tên chiến dịch"
                    required
                    placeholder="Nhập tên chiến dịch khuyến mãi..."
                    value={formData.name}
                    onChange={(e) => handleFieldChange('name', e.target.value)}
                  />
                  <FormSelect
                    label="Loại khuyến mãi"
                    options={DISCOUNT_TYPE_OPTIONS}
                    value={formData.isPercentage ? 1 : 0}
                    onSelect={(val) => handleFieldChange('isPercentage', val === 1)}
                  />
                  <FormInput
                    label={formData.isPercentage ? 'Mức giảm (%)' : 'Mức giảm (VNĐ)'}
                    type="number"
                    required
                    placeholder="Nhập giá trị giảm..."
                    value={formData.discountValue}
                    onChange={(e) =>
                      handleFieldChange('discountValue', parseFloat(e.target.value) || 0)
                    }
                  />

                  <div className="grid grid-cols-2 gap-4">
                    <CustomDatePicker
                      label="Bắt đầu từ"
                      value={new Date(formData.startDate)}
                      onChange={(d) => handleFieldChange('startDate', d?.toISOString())}
                    />
                    <CustomDatePicker
                      label="Kết thúc vào"
                      value={new Date(formData.endDate)}
                      onChange={(d) => handleFieldChange('endDate', d?.toISOString())}
                    />
                  </div>
                </div>
                <div className="mt-6">
                  <FormTextarea
                    label="Mô tả chiến dịch"
                    placeholder="Mô tả chi tiết và thể lệ chương trình khuyến mãi..."
                    value={formData.description || ''}
                    onChange={(e: any) => handleFieldChange('description', e.target.value)}
                  />
                </div>
              </FormSection>
            </div>
          )}

          {/* ================= TAB 2: CHỌN SẢN PHẨM ================= */}
          {activeTab === 'products' && (
            <div className="animate-in fade-in slide-in-from-bottom-4 duration-300">
              <FormSection title="Danh Sách Biến Thể">
                <div className="flex flex-col border border-slate-200 rounded-xl overflow-hidden bg-white shadow-sm">
                  {/* THANH CÔNG CỤ */}
                  <div className="flex flex-col sm:flex-row justify-between items-center gap-4 bg-slate-50 p-4 border-b border-slate-200">
                    <button
                      type="button"
                      onClick={handleSelectAllFiltered}
                      className="flex items-center gap-2 px-4 py-2 bg-white rounded-lg border border-slate-200 hover:border-yellow-400 hover:bg-yellow-50 transition-all shadow-sm cursor-pointer"
                    >
                      {isAllFilteredSelected ? (
                        <CheckSquare className="text-yellow-500" size={18} />
                      ) : (
                        <Square className="text-slate-400" size={18} />
                      )}
                      <span className="text-sm font-bold text-slate-700">
                        {isAllFilteredSelected ? 'Bỏ chọn tất cả' : 'Chọn tất cả'}{' '}
                        <span className="font-normal text-slate-500">
                          ({filteredVariants.length})
                        </span>
                      </span>
                    </button>

                    <div className="relative w-full sm:w-96">
                      <Search
                        className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
                        size={16}
                      />
                      <input
                        type="text"
                        placeholder="Tìm kiếm sản phẩm theo tên, mã SKU..."
                        className="w-full pl-9 pr-4 py-2.5 text-sm font-medium border border-slate-200 rounded-xl focus:outline-none focus:border-yellow-400 focus:ring-4 focus:ring-yellow-400/20 transition-all"
                        value={variantSearch}
                        onChange={(e) => setVariantSearch(e.target.value)}
                        onKeyDown={(e) => e.key === 'Enter' && e.preventDefault()}
                      />
                    </div>
                  </div>

                  {/* DANH SÁCH SẢN PHẨM */}
                  <div className="max-h-112.5 overflow-y-auto divide-y divide-slate-100 bg-slate-50/30">
                    {filteredVariants.length === 0 ? (
                      <div className="p-12 text-center flex flex-col items-center">
                        <Box size={40} className="text-slate-200 mb-3" />
                        <p className="text-slate-400 text-sm font-medium">
                          Không tìm thấy sản phẩm nào phù hợp với từ khóa.
                        </p>
                      </div>
                    ) : (
                      filteredVariants.map((v) => {
                        const isSelected = formData.variantIds?.includes(v.id);
                        const defaultPriceInfo =
                          v.prices?.find((p) => p.isDefault) || v.prices?.[0];

                        return (
                          <div
                            key={v.id}
                            onClick={() => toggleVariant(v.id)}
                            className={`flex items-center gap-4 p-3.5 cursor-pointer transition-colors hover:bg-slate-50 ${isSelected ? 'bg-yellow-50/50' : ''}`}
                          >
                            <div className="shrink-0 ml-2">
                              {isSelected ? (
                                <CheckSquare className="text-yellow-500" size={20} />
                              ) : (
                                <Square className="text-slate-300" size={20} />
                              )}
                            </div>
                            <div className="w-11 h-11 rounded-lg border border-slate-200 flex items-center justify-center overflow-hidden bg-white shrink-0 shadow-sm">
                              {v.imagePath ? (
                                <img
                                  src={v.imagePath}
                                  alt=""
                                  className="w-full h-full object-cover"
                                />
                              ) : (
                                <Box size={20} className="text-slate-300" />
                              )}
                            </div>
                            <div className="flex-1 min-w-0 flex flex-col">
                              <h4 className="text-[14px] font-bold text-slate-800 truncate leading-snug">
                                {v.name}
                              </h4>
                              <div className="flex items-center gap-2 mt-1">
                                <span className="text-[10px] bg-slate-100 text-slate-500 px-1.5 py-0.5 rounded uppercase font-bold tracking-wider">
                                  {v.code}
                                </span>
                              </div>
                            </div>
                            <div className="text-right pr-4 shrink-0 flex flex-col items-end">
                              <span className="text-xs text-slate-400 font-bold uppercase mb-0.5">
                                Giá Bán
                              </span>

                              {defaultPriceInfo ? (
                                <span className="text-[14px] font-black text-slate-700">
                                  {defaultPriceInfo.price.toLocaleString('vi-VN')} ₫
                                  <span className="text-[11px] font-medium text-slate-400 ml-1">
                                    / {defaultPriceInfo.uoMName}
                                  </span>
                                </span>
                              ) : (
                                <span className="text-[12px] italic text-amber-500 bg-amber-50 px-2 py-0.5 rounded">
                                  Chưa cài giá
                                </span>
                              )}
                            </div>
                          </div>
                        );
                      })
                    )}
                  </div>
                </div>
              </FormSection>
            </div>
          )}

          {/* ================= FOOTER ================= */}
          <div className="flex justify-between items-center pt-6 border-t border-slate-100">
            {activeTab === 'products' ? (
              <button
                type="button"
                onClick={() => setActiveTab('info')}
                className="px-6 py-2.5 rounded-xl font-bold text-sm text-slate-600 bg-slate-100 hover:bg-slate-200 transition-colors cursor-pointer"
              >
                Quay lại Thông tin
              </button>
            ) : (
              <div></div>
            )}

            {activeTab === 'info' ? (
              <button
                type="button"
                onClick={() => setActiveTab('products')}
                className="flex items-center gap-2 px-6 py-2.5 rounded-xl font-bold text-sm text-slate-800 bg-yellow-400 hover:bg-yellow-500 transition-colors shadow-sm shadow-yellow-200 cursor-pointer"
              >
                Tiếp tục chọn Sản phẩm <ChevronRight size={18} />
              </button>
            ) : (
              <SubmitButton
                loading={loading}
                isEditMode={isEditMode}
                icon={isEditMode ? Save : Plus}
              />
            )}
          </div>
        </form>
      </FormCard>
    </PageContainer>
  );
};

export default PromotionCampaignForm;
