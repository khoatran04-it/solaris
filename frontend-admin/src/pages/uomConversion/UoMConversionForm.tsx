import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Repeat,
  Save,
  Plus,
  ArrowRight,
  PackageOpen,
  LayoutTemplate,
  Sparkles,
  CheckCircle2,
  ChevronLeft,
} from 'lucide-react';

// API & Types
import { uomConversionApi } from '../../api/uomConversionApi';
import { uomApi } from '../../api/uomApi';
import { productApi } from '../../api/productApi';
import { UoMConversionPayload } from '../../types/uomConversion';
import { Product } from '../../types/product';

// Shared UI
import { Toast } from '../../components/commons/Toast';
import {
  PageContainer,
  FormCard,
  FormInput,
  FormHeader,
  FormSection,
  FormSelect,
  SubmitButton,
} from '../../components/commons/FormUI';
import { TabGroup, TabButton } from '../../components/commons/TabUI';

const INITIAL_STATE: UoMConversionPayload = {
  fromUoMId: 0,
  toUoMId: 0,
  conversionFactor: 1,
  productId: null,
  isActive: true,
};

const STATUS_OPTIONS = [
  { label: 'Hoạt động', value: 1 },
  { label: 'Tạm khóa', value: 0 },
];

const UoMConversionForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);

  // --- STATES ---
  const [formData, setFormData] = useState<UoMConversionPayload>(INITIAL_STATE);
  const [isStandardMode, setIsStandardMode] = useState<boolean>(true);
  const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
  const [loading, setLoading] = useState(false);

  // --- DROPDOWN DATA ---
  const [rawProducts, setRawProducts] = useState<Product[]>([]);
  const [uomOptions, setUomOptions] = useState<{ label: string; value: number }[]>([]);
  const [productOptions, setProductOptions] = useState<{ label: string; value: number }[]>([]);

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
    // Tải danh mục UoM và Product song song
    Promise.all([uomApi.getAllList(), productApi.getAllList()])
      .then(([uoms, products]) => {
        setUomOptions(uoms.map((u) => ({ label: `${u.name} (${u.code})`, value: u.id })));
        setRawProducts(products || []);
        setProductOptions(products.map((p) => ({ label: `${p.code} - ${p.name}`, value: p.id })));
      })
      .catch(() => showToast('warning', 'Không tải được danh mục bổ trợ từ máy chủ'));

    // Tải chi tiết Edit nếu có ID
    if (isEditMode && id) {
      uomConversionApi
        .getById(Number(id))
        .then((res) => {
          setFormData({
            fromUoMId: res.fromUoMId,
            toUoMId: res.toUoMId,
            conversionFactor: res.conversionFactor,
            productId: res.productId,
            isActive: res.isActive,
          });
          setIsStandardMode(res.productId === null);
        })
        .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU TỶ LỆ QUY ĐỔI'));
    }
  }, [id, isEditMode]);

  // --- HELPERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const handleFieldChange = (field: keyof UoMConversionPayload, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  // Tự động gán và khóa đơn vị cơ sở khi chọn sản phẩm
  const handleProductSelect = (productId: number) => {
    handleFieldChange('productId', productId);
    const prod = rawProducts.find((p) => p.id === productId);
    if (prod?.baseUoMId) {
      handleFieldChange('toUoMId', prod.baseUoMId);
      if (formData.fromUoMId === prod.baseUoMId) {
        handleFieldChange('fromUoMId', 0);
      }
    }
  };

  // --- VALIDATION ---
  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.fromUoMId) newErrors.fromUoMId = 'Chọn đơn vị nguồn.';
    if (!formData.toUoMId) newErrors.toUoMId = 'Chọn đơn vị đích (gốc).';

    if (formData.fromUoMId && formData.toUoMId && formData.fromUoMId === formData.toUoMId) {
      newErrors.toUoMId = 'Đơn vị đích không được trùng đơn vị nguồn.';
    }

    if (!formData.conversionFactor || formData.conversionFactor <= 0) {
      newErrors.conversionFactor = 'Hệ số quy đổi phải là số dương lớn hơn 0.';
    }

    if (!isStandardMode) {
      if (!formData.productId) {
        newErrors.productId = 'Vui lòng chọn sản phẩm áp dụng.';
      } else if (selectedProduct?.baseUoMId && formData.toUoMId !== selectedProduct.baseUoMId) {
        newErrors.toUoMId = `Đơn vị đích bắt buộc phải là đơn vị cơ sở của sản phẩm (${selectedProduct.baseUoMName}).`;
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // --- SUBMIT ---
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return showToast('warning', 'Vui lòng kiểm tra lại thông tin bị lỗi!');

    setLoading(true);
    try {
      const cleanPayload: UoMConversionPayload = {
        ...formData,
        productId: isStandardMode ? null : formData.productId,
      };

      if (isEditMode && id) {
        await uomConversionApi.update(Number(id), cleanPayload);
        showToast('success', 'CẬP NHẬT TỶ LỆ QUY ĐỔI THÀNH CÔNG');
      } else {
        await uomConversionApi.create(cleanPayload);
        showToast('success', 'THÊM MỚI TỶ LỆ QUY ĐỔI THÀNH CÔNG');
      }
      setTimeout(() => navigate('/uom-conversions'), 1000);
    } catch (error: any) {
      showToast('error', error.response?.data?.message || 'CÓ LỖI XẢY RA KHI LƯU DỮ LIỆU');
    } finally {
      setLoading(false);
    }
  };

  // Lấy nhãn hiển thị cho đơn vị tính
  const selectedFromUoM = uomOptions.find((u) => u.value === formData.fromUoMId);
  const selectedToUoM = uomOptions.find((u) => u.value === formData.toUoMId);
  const selectedProduct = rawProducts.find((p) => p.id === formData.productId);

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title={isEditMode ? 'Chỉnh Sửa Tỷ Lệ Quy Đổi' : 'Thêm Mới Tỷ Lệ Quy Đổi'}
        subtitle="Định nghĩa công thức toán học và hệ số chuyển đổi giữa các đơn vị tính"
        onBack={() => navigate('/uom-conversions')}
        icon={Repeat}
      />

      {/* TABS ĐIỀU HƯỚNG THEO THEME SOLARIS */}
      <div className="mb-5 flex items-center justify-between">
        <TabGroup>
          <TabButton
            active={isStandardMode}
            onClick={() => {
              setIsStandardMode(true);
              handleFieldChange('productId', null);
            }}
            label="Tiêu chuẩn toàn cục"
            icon={LayoutTemplate}
          />
          <TabButton
            active={!isStandardMode}
            onClick={() => setIsStandardMode(false)}
            label="Đặc thù sản phẩm"
            icon={PackageOpen}
          />
        </TabGroup>
      </div>

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-8">
          {/* BANNER HƯỚNG DẪN NGHIỆP VỤ */}
          <div className="p-4 bg-amber-50/70 border border-amber-200/80 rounded-2xl flex items-start gap-3.5 text-sm text-slate-700">
            {isStandardMode ? (
              <>
                <Sparkles className="w-5 h-5 text-amber-500 shrink-0 mt-0.5" />
                <div>
                  <h4 className="font-bold text-slate-800">Quy đổi tiêu chuẩn (Toàn cục)</h4>
                  <p className="text-xs text-slate-600 mt-0.5 leading-relaxed">
                    Áp dụng chung cho tất cả sản phẩm dùng chung nhóm đo lường trong hệ thống (Ví dụ:
                    1 Kg = 1.000 g, 1 Thùng = 24 Lon). Mọi sản phẩm có cặp đơn vị tính này đều tự
                    động thừa hưởng công thức mà không cần khai báo lại.
                  </p>
                </div>
              </>
            ) : (
              <>
                <PackageOpen className="w-5 h-5 text-amber-600 shrink-0 mt-0.5" />
                <div>
                  <h4 className="font-bold text-slate-800">Quy đổi đặc thù riêng cho sản phẩm</h4>
                  <p className="text-xs text-slate-600 mt-0.5 leading-relaxed">
                    Dành cho các đơn vị bao bì đóng gói có số lượng quy cách khác nhau theo từng mặt
                    hàng (Ví dụ: 1 Thùng Mì Omachi = 30 Gói, nhưng 1 Thùng Bia Saigon = 24 Lon). Hệ
                    số này chỉ áp dụng riêng cho sản phẩm bạn chỉ định.
                  </p>
                </div>
              </>
            )}
          </div>

          {/* TAB 2: CHỌN SẢN PHẨM ÁP DỤNG NẾU LÀ ĐẶC THÙ */}
          {!isStandardMode && (
            <FormSection title="Sản Phẩm Áp Dụng">
              <div className="w-full lg:w-2/3">
                <FormSelect
                  label="Chọn sản phẩm áp dụng"
                  required
                  showSearch
                  placeholder="Tìm theo mã hoặc tên sản phẩm..."
                  options={productOptions}
                  value={formData.productId || 0}
                  error={errors.productId}
                  onSelect={(val) => handleProductSelect(Number(val))}
                  disabled={isEditMode}
                />

                {selectedProduct && (
                  <div className="mt-3 p-3.5 bg-slate-50 border border-slate-200 rounded-xl flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className="px-2 py-0.5 bg-slate-200 text-slate-700 rounded text-xs font-bold">
                        {selectedProduct.code}
                      </span>
                      <span className="font-bold text-slate-800 text-sm">
                        {selectedProduct.name}
                      </span>
                    </div>
                    <div className="flex items-center gap-1.5 text-xs font-bold text-amber-800 bg-amber-50 px-2.5 py-1 rounded-lg border border-amber-200 shadow-2xs">
                      <span>Đơn vị cơ sở:</span>
                      <span className="text-amber-600 uppercase font-black">
                        {selectedProduct.baseUoMName || 'Chưa gán'}
                      </span>
                    </div>
                  </div>
                )}
              </div>
            </FormSection>
          )}

          {/* KHU VỰC CÔNG THỨC TOÁN HỌC */}
          <FormSection title="Công Thức Toán Học">
            {/* PREVIEW CÔNG THỨC TOÁN HỌC TRỰC QUAN */}
            {selectedFromUoM && selectedToUoM && formData.conversionFactor > 0 && (
              <div className="mb-5 p-4 bg-gradient-to-r from-amber-50/80 via-yellow-50/40 to-slate-50 border border-amber-200/90 rounded-2xl flex flex-col sm:flex-row items-center justify-between gap-3 text-slate-800 shadow-2xs">
                <div className="flex items-center gap-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CheckCircle2 size={16} className="text-emerald-500" />
                  <span>Xem trước công thức:</span>
                </div>
                <div className="flex items-center gap-2.5 font-black text-sm sm:text-base">
                  <span className="px-3 py-1 bg-white text-slate-800 rounded-xl border border-slate-200 shadow-xs">
                    1 {selectedFromUoM.label.split(' ')[0]}
                  </span>
                  <span className="text-amber-500 text-lg font-bold">=</span>
                  <span className="px-3.5 py-1 bg-amber-400 text-slate-900 rounded-xl shadow-xs">
                    {formData.conversionFactor.toLocaleString('vi-VN')}{' '}
                    {selectedToUoM.label.split(' ')[0]}
                  </span>
                </div>
              </div>
            )}

            <div className="p-6 bg-slate-50/70 rounded-2xl border border-slate-200 flex flex-col gap-4">
              <div className="grid grid-cols-1 lg:grid-cols-11 gap-4 items-center">
                {/* CỘT 1: TỪ ĐƠN VỊ */}
                <div className="lg:col-span-4 w-full">
                  <FormSelect
                    label="Từ đơn vị tính (Quy cách lớn / Bao bì)"
                    required
                    showSearch
                    placeholder="VD: Thùng"
                    options={
                      !isStandardMode && selectedProduct?.baseUoMId
                        ? uomOptions.filter((u) => u.value !== selectedProduct.baseUoMId)
                        : uomOptions
                    }
                    value={formData.fromUoMId}
                    error={errors.fromUoMId}
                    onSelect={(val) => handleFieldChange('fromUoMId', Number(val))}
                  />
                  <p className="text-[11px] text-slate-400 mt-1 font-medium italic">
                    * Đơn vị đóng gói lớn hơn (VD: 1 Thùng)
                  </p>
                </div>

                {/* MŨI TÊN CHỈ HƯỚNG */}
                <div className="hidden lg:flex lg:col-span-1 justify-center pt-2 text-amber-500">
                  <ArrowRight size={22} strokeWidth={2.5} />
                </div>

                {/* CỘT 2: HỆ SỐ QUY ĐỔI */}
                <div className="lg:col-span-2 w-full">
                  <FormInput
                    label="Hệ số quy đổi"
                    required
                    placeholder="VD: 24"
                    type="number"
                    value={formData.conversionFactor}
                    error={errors.conversionFactor}
                    disabled={loading}
                    onChange={(e) => handleFieldChange('conversionFactor', Number(e.target.value))}
                  />
                  <p className="text-[11px] text-slate-400 mt-1 font-medium italic">
                    * Số lượng chứa bên trong
                  </p>
                </div>

                {/* DẤU BẰNG / MŨI TÊN */}
                <div className="hidden lg:flex lg:col-span-1 justify-center pt-2 text-amber-500">
                  <ArrowRight size={22} strokeWidth={2.5} />
                </div>

                {/* CỘT 3: ĐẾN ĐƠN VỊ */}
                <div className="lg:col-span-3 w-full">
                  <FormSelect
                    label="Đến đơn vị tính (Đơn vị con / Cơ sở)"
                    required
                    showSearch={isStandardMode || !selectedProduct?.baseUoMId}
                    disabled={!isStandardMode && !!selectedProduct?.baseUoMId}
                    placeholder="VD: Lon"
                    options={
                      !isStandardMode && selectedProduct?.baseUoMId
                        ? [
                            {
                              label: `${selectedProduct.baseUoMName || 'ĐV cơ sở'} (Đơn vị cơ sở của sản phẩm)`,
                              value: selectedProduct.baseUoMId,
                            },
                          ]
                        : uomOptions
                    }
                    value={
                      !isStandardMode && selectedProduct?.baseUoMId
                        ? selectedProduct.baseUoMId
                        : formData.toUoMId
                    }
                    error={errors.toUoMId}
                    onSelect={(val) => handleFieldChange('toUoMId', Number(val))}
                  />
                  <p className="text-[11px] text-slate-400 mt-1 font-medium italic">
                    {!isStandardMode && selectedProduct?.baseUoMId
                      ? `* Cố định theo ĐVT cơ sở (${selectedProduct.baseUoMName}) của sản phẩm`
                      : '* Đơn vị đích (thường là đơn vị cơ sở)'}
                  </p>
                </div>
              </div>
            </div>
          </FormSection>

          {/* KHU VỰC CÀI ĐẶT */}
          <FormSection title="Cài Đặt Hoạt Động">
            <div className="w-full lg:w-1/2">
              <FormSelect
                label="Trạng thái áp dụng"
                required
                value={formData.isActive ? 1 : 0}
                options={STATUS_OPTIONS}
                onSelect={(val) => handleFieldChange('isActive', val === 1)}
              />
            </div>
          </FormSection>

          {/* FOOTER BUTTONS */}
          <div className="flex justify-between items-center pt-6 border-t border-slate-100 mt-2">
            <button
              type="button"
              onClick={() => navigate('/uom-conversions')}
              className="px-6 py-2.5 rounded-xl font-bold text-sm text-slate-600 bg-slate-100 hover:bg-slate-200 transition-colors flex items-center gap-2"
            >
              <ChevronLeft size={16} />
              Quay lại danh sách
            </button>

            <SubmitButton
              loading={loading}
              isEditMode={isEditMode}
              icon={isEditMode ? Save : Plus}
            />
          </div>
        </form>
      </FormCard>
    </PageContainer>
  );
};

export default UoMConversionForm;
