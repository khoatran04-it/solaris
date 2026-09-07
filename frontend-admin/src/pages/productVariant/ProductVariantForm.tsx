import React, { useEffect, useState, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Package,
  Save,
  Plus,
  Layers,
  Info,
  ChevronRight,
  Banknote,
  Trash2,
  Star,
  AlertCircle,
  Repeat,
} from 'lucide-react';

// API & Types
import { productVariantApi } from '../../api/productVariantApi';
import { productApi } from '../../api/productApi';
import { uomApi } from '../../api/uomApi';
import { uomConversionApi } from '../../api/uomConversionApi';
import { ProductVariantPayload, VariantPriceInput } from '../../types/productVariant';
import { Product } from '../../types/product';
import { UoMConversion } from '../../types/uomConversion';

// Modals
import { QuickUoMConversionModal } from '../../components/modals/QuickUoMConversionModal';

// Shared UI Components
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
import { TabGroup, TabButton } from '../../components/commons/TabUI';

const INITIAL_STATE: ProductVariantPayload = {
  code: '',
  name: '',
  description: '',
  imagePath: '',
  inventoryGuideline: 0,
  isActive: true,
  productId: 0,
  grossWeightKg: undefined,
  lengthCm: undefined,
  widthCm: undefined,
  heightCm: undefined,
  unitCbm: undefined,
  attributes: [],
  prices: [], // Khởi tạo mảng giá
};

const STATUS_OPTIONS = [
  { label: 'Hoạt động', value: 1 },
  { label: 'Tạm khóa', value: 0 },
];

interface AttributeDef {
  id: number;
  name: string;
  isRequired?: boolean;
}

type TabType = 'info' | 'attributes' | 'pricing';

const ProductVariantForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);

  // --- STATES ---
  const [formData, setFormData] = useState<ProductVariantPayload>(INITIAL_STATE);
  const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
  const [loading, setLoading] = useState(false);

  // --- TABS & DROPDOWN OPTIONS ---
  const [activeTab, setActiveTab] = useState<TabType>('info');
  const [rawProducts, setRawProducts] = useState<Product[]>([]);
  const [productOptions, setProductOptions] = useState<{ label: string; value: number }[]>([]);
  const [uomOptions, setUomOptions] = useState<{ label: string; value: number }[]>([]); // Data Đơn vị tính
  const [uomConversions, setUomConversions] = useState<UoMConversion[]>([]);
  const [dynamicAttributes, setDynamicAttributes] = useState<AttributeDef[]>([]);

  // Modal tạo nhanh tỷ lệ quy đổi đặc thù sản phẩm
  const [quickConvModal, setQuickConvModal] = useState<{
    isOpen: boolean;
    fromUoMId?: number;
    targetRowIndex?: number;
  }>({
    isOpen: false,
    fromUoMId: undefined,
    targetRowIndex: undefined,
  });

  // Sản phẩm gốc đang được chọn
  const currentProd = rawProducts.find((p) => p.id === formData.productId);

  // Đánh dấu đã tự động điền ĐVT cơ sở cho sản phẩm này hay chưa (tránh ghi đè khi user đổi tab)
  const autoInitializedBaseUoMRef = useRef<number | null>(null);

  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'warning' | 'error';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- EFFECT 1: Init Data ---
  useEffect(() => {
    // Tải SP gốc, Đơn vị tính và Quy đổi UoM song song
    Promise.all([
      productApi.getAllList(),
      uomApi.getAllList(), // API lấy danh sách UoM (Kg, Thùng, Hộp...)
      uomConversionApi.getAllList(true).catch(() => []), // API lấy danh sách Quy đổi ĐVT đang hoạt động
    ])
      .then(([products, uoms, conversions]) => {
        setRawProducts(products || []);
        setProductOptions((products || []).map((p: any) => ({ label: p.code ? `${p.name} - ${p.code}` : p.name, value: p.id })));
        setUomOptions((uoms || []).map((u: any) => ({ label: u.name, value: u.id })));
        setUomConversions(conversions || []);
      })
      .catch(() =>
        showToast('warning', 'Không tải được danh mục bổ trợ (Sản phẩm / Đơn vị tính / Quy đổi)')
      );

    // Lấy chi tiết Edit
    if (isEditMode && id) {
      productVariantApi
        .getById(Number(id))
        .then((res) => {
          setFormData({
            code: res.code || '',
            name: res.name || '',
            description: res.description || '',
            imagePath: res.imagePath || '',
            inventoryGuideline: res.inventoryGuideline || 0,
            isActive: res.isActive,
            productId: res.productId,
            grossWeightKg: res.grossWeightKg,
            lengthCm: res.lengthCm,
            widthCm: res.widthCm,
            heightCm: res.heightCm,
            unitCbm: res.unitCbm,
            attributes: res.attributes.map((a) => ({
              attributeDefinitionId: a.attributeDefinitionId!,
              attributeValue: a.attributeValue,
            })),
            // Map bảng giá
            prices: res.prices.map((p) => ({
              uoMId: p.uoMId,
              price: p.price,
              isDefault: p.isDefault,
            })),
          });
        })
        .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU BIẾN THỂ'));
    }
  }, [id, isEditMode]);

  // --- EFFECT 2: Xử lý Form Động (EAV Pattern) ---
  useEffect(() => {
    if (formData.productId > 0) {
      productApi
        .getAttributesConfig(formData.productId)
        .then((attrs) => setDynamicAttributes(attrs))
        .catch(() => setDynamicAttributes([]));
    } else {
      setDynamicAttributes([]);
    }
  }, [formData.productId]);

  // --- EFFECT 3: Tự động nhận diện Đơn vị tính cơ sở của sản phẩm và điền vào dòng đầu tiên khi chuyển sang Tab Quy cách bán ---
  useEffect(() => {
    if (activeTab === 'pricing' && formData.productId > 0 && !isEditMode) {
      if (autoInitializedBaseUoMRef.current !== formData.productId) {
        const prod = rawProducts.find((p) => p.id === formData.productId);
        if (prod?.baseUoMId) {
          setFormData((prev) => {
            // 1. Nếu bảng giá đang trống hoàn toàn:
            if (prev.prices.length === 0) {
              return {
                ...prev,
                prices: [{ uoMId: prod.baseUoMId, price: 0, isDefault: true }],
              };
            }
            // 2. Nếu chỉ có 1 dòng mà chưa chọn ĐVT hoặc chưa nhập giá:
            if (
              prev.prices.length === 1 &&
              (prev.prices[0].uoMId === 0 || prev.prices[0].price === 0)
            ) {
              const updated = [...prev.prices];
              updated[0] = { ...updated[0], uoMId: prod.baseUoMId, isDefault: true };
              return { ...prev, prices: updated };
            }
            return prev;
          });
          autoInitializedBaseUoMRef.current = formData.productId;
        }
      }
    }
  }, [activeTab, formData.productId, rawProducts, isEditMode]);

  // --- HELPERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const handleFieldChange = (field: keyof ProductVariantPayload, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  const handleDimensionChange = (
    field: 'lengthCm' | 'widthCm' | 'heightCm',
    val: number | undefined
  ) => {
    setFormData((prev) => {
      const next = { ...prev, [field]: val };
      const l = field === 'lengthCm' ? val : prev.lengthCm;
      const w = field === 'widthCm' ? val : prev.widthCm;
      const h = field === 'heightCm' ? val : prev.heightCm;
      if (l && w && h && l > 0 && w > 0 && h > 0) {
        next.unitCbm = Math.round(((l * w * h) / 1000000) * 10000) / 10000;
      }
      return next;
    });
  };

  // Helper: Cập nhật Thuộc tính
  const handleDynamicAttrChange = (definitionId: number, value: string) => {
    setFormData((prev) => {
      const newAttrs = [...prev.attributes];
      const index = newAttrs.findIndex((a) => a.attributeDefinitionId === definitionId);
      if (index >= 0) newAttrs[index].attributeValue = value;
      else newAttrs.push({ attributeDefinitionId: definitionId, attributeValue: value });
      return { ...prev, attributes: newAttrs };
    });
  };
  const getDynamicAttrValue = (definitionId: number) =>
    formData.attributes.find((a) => a.attributeDefinitionId === definitionId)?.attributeValue || '';

  // Helpers: Xử lý bảng giá biến thể
  const handleAddPriceRow = () => {
    setFormData((prev) => ({
      ...prev,
      prices: [...prev.prices, { uoMId: 0, price: 0, isDefault: prev.prices.length === 0 }], // Dòng đầu tiên tự auto làm Mặc định
    }));
  };

  const handleRemovePriceRow = (index: number) => {
    setFormData((prev) => {
      const newPrices = prev.prices.filter((_, i) => i !== index);
      // Nếu lỡ xóa trúng dòng mặc định, gán tạm dòng đầu làm mặc định
      if (newPrices.length > 0 && !newPrices.some((p) => p.isDefault)) {
        newPrices[0].isDefault = true;
      }
      return { ...prev, prices: newPrices };
    });
  };

  const handleUpdatePriceRow = (index: number, field: keyof VariantPriceInput, value: any) => {
    setFormData((prev) => {
      const newPrices = [...prev.prices];
      newPrices[index] = { ...newPrices[index], [field]: value };
      return { ...prev, prices: newPrices };
    });
  };

  const handleSetDefaultPrice = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      prices: prev.prices.map((p, i) => ({ ...p, isDefault: i === index })),
    }));
  };

  // Mở modal cấu hình quy đổi đặc thù nhanh
  const handleOpenQuickConversionModal = (uoMId?: number, rowIndex?: number) => {
    if (!formData.productId) {
      showToast('warning', 'Vui lòng chọn Sản phẩm gốc ở Tab 1 trước khi cấu hình quy đổi!');
      setActiveTab('info');
      return;
    }
    setQuickConvModal({
      isOpen: true,
      fromUoMId: uoMId,
      targetRowIndex: rowIndex,
    });
  };

  // Callback khi tạo quy đổi đặc thù thành công từ Modal
  const handleQuickConversionSuccess = (newConv: UoMConversion) => {
    setUomConversions((prev) => {
      const filtered = prev.filter((c) => c.id !== newConv.id);
      return [...filtered, newConv];
    });

    setFormData((prev) => {
      const newPrices = [...prev.prices];
      const targetIdx = quickConvModal.targetRowIndex;

      if (targetIdx !== undefined && targetIdx >= 0 && targetIdx < newPrices.length) {
        // Trường hợp 1: Mở từ một dòng cụ thể -> Cập nhật dòng đó
        newPrices[targetIdx] = {
          ...newPrices[targetIdx],
          uoMId: newConv.fromUoMId,
        };
      } else {
        // Trường hợp 2: Mở từ nút tổng quát bên dưới bảng
        const emptyRowIdx = newPrices.findIndex((p) => !p.uoMId || p.uoMId === 0);
        if (emptyRowIdx !== -1) {
          newPrices[emptyRowIdx] = {
            ...newPrices[emptyRowIdx],
            uoMId: newConv.fromUoMId,
          };
        } else {
          const exists = newPrices.some((p) => p.uoMId === newConv.fromUoMId);
          if (!exists) {
            newPrices.push({
              uoMId: newConv.fromUoMId,
              price: 0,
              discountedPrice: 0,
              discountPercent: 0,
              isDefault: newPrices.length === 0,
            });
          }
        }
      }
      return { ...prev, prices: newPrices };
    });

    showToast(
      'success',
      `Đã lưu quy đổi: 1 ${newConv.fromUoMName || 'ĐVT'} = ${newConv.conversionFactor} ${newConv.toUoMName || 'ĐVT'} và tự động điền vào quy cách bán!`
    );
    // Tự động giải phóng lỗi bảng giá nếu người dùng vừa cấu hình xong
    setErrors((prev) => {
      const next = { ...prev };
      delete next.prices;
      return next;
    });
  };

  // --- VALIDATION ---
  const validateForm = (): { isValid: boolean; errorMessage?: string } => {
    const newErrors: Record<string, string> = {};

    // Tab 1 Validate
    if (!formData.productId) newErrors.productId = 'Vui lòng chọn Sản phẩm gốc.';
    if (!formData.code.trim()) newErrors.code = 'Vui lòng nhập mã SKU.';
    if (!formData.name.trim()) newErrors.name = 'Vui lòng nhập tên biến thể.';
    if (formData.inventoryGuideline < 0) newErrors.inventoryGuideline = 'Tồn kho không hợp lệ.';

    // Tab 2 Validate
    dynamicAttributes.forEach((def) => {
      if (def.isRequired && !getDynamicAttrValue(def.id).trim()) {
        newErrors[`attr_${def.id}`] = `Vui lòng nhập ${def.name}.`;
      }
    });

    // Tab 3 Validate
    if (formData.prices.length === 0) {
      newErrors.prices = 'Vui lòng thiết lập ít nhất 1 quy cách bán hàng.';
    } else {
      const hasInvalidRow = formData.prices.some((p) => p.uoMId === 0 || p.price < 0);
      if (hasInvalidRow) newErrors.prices = 'Vui lòng chọn Đơn vị tính và nhập giá tiền hợp lệ.';

      const hasDefault = formData.prices.some((p) => p.isDefault);
      if (!hasDefault) newErrors.prices = 'Vui lòng chọn 1 đơn vị tính làm mặc định hiển thị.';

      // Kiểm tra trùng Đơn vị tính
      const uomSet = new Set(formData.prices.map((p) => p.uoMId));
      if (uomSet.size !== formData.prices.length) {
        newErrors.prices = 'Có đơn vị tính đang bị trùng lặp. Vui lòng kiểm tra lại.';
      }

      // CHẶN LUỒNG NGHIỆP VỤ: Không cho lưu nếu có quy cách bán chưa cấu hình quy đổi về ĐVT cơ sở
      if (currentProd) {
        const effectiveBaseUoMId =
          currentProd.baseUoMId ||
          uomConversions.find(
            (c) =>
              c.productId === formData.productId ||
              (!c.productId && formData.prices.some((p) => p.uoMId === c.fromUoMId))
          )?.toUoMId ||
          (formData.prices.length === 1 ? formData.prices[0].uoMId : undefined);

        if (effectiveBaseUoMId) {
          const unconfiguredRow = formData.prices.find((p) => {
            if (!p.uoMId || p.uoMId === 0) return false;
            // Nếu chính là đơn vị cơ sở thì luôn hợp lệ
            if (p.uoMId === effectiveBaseUoMId) return false;
            // Kiểm tra xem có quy đổi đặc thù cho sản phẩm này HOẶC quy đổi tiêu chuẩn toàn cục hay không
            const hasConv = uomConversions.some(
              (c) =>
                c.isActive &&
                (c.productId === formData.productId || !c.productId) &&
                c.fromUoMId === p.uoMId
            );
            return !hasConv;
          });

          if (unconfiguredRow) {
            const uomLabel =
              uomOptions.find((u) => u.value === unconfiguredRow.uoMId)?.label?.split(' ')[0] ||
              'được chọn';
            newErrors.prices = `Quy cách bán '${uomLabel}' chưa được cấu hình tỷ lệ quy đổi về đơn vị cơ sở (${currentProd.baseUoMName || 'cơ sở'}). Vui lòng bấm 'Cấu hình ngay' trên dòng đó trước khi lưu!`;
          }
        }
      }
    }

    setErrors(newErrors);

    // Auto Focus chuyển Tab khi có lỗi
    if (Object.keys(newErrors).length > 0) {
      if (newErrors.prices) setActiveTab('pricing');
      else if (Object.keys(newErrors).some((k) => k.startsWith('attr_')))
        setActiveTab('attributes');
      else setActiveTab('info');

      const firstErrorMsg =
        newErrors.prices ||
        newErrors.productId ||
        newErrors.code ||
        newErrors.name ||
        'Vui lòng kiểm tra lại các trường báo đỏ!';
      return { isValid: false, errorMessage: firstErrorMsg };
    }

    return { isValid: true };
  };

  // --- SUBMIT ---
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const validation = validateForm();
    if (!validation.isValid) {
      return showToast('warning', validation.errorMessage || 'Vui lòng kiểm tra lại các trường báo đỏ!');
    }

    setLoading(true);
    try {
      const cleanPayload: ProductVariantPayload = {
        ...formData,
        code: formData.code.trim().toUpperCase(),
        name: formData.name.trim(),
        imagePath: formData.imagePath?.trim() || null,
        description: formData.description?.trim() || null,
        attributes: formData.attributes.filter((a) => a.attributeValue.trim() !== ''),
        prices: formData.prices,
      };

      if (isEditMode && id) {
        await productVariantApi.update(Number(id), cleanPayload);
        showToast('success', 'CẬP NHẬT BIẾN THỂ THÀNH CÔNG');
      } else {
        await productVariantApi.create(cleanPayload);
        showToast('success', 'THÊM MỚI BIẾN THỂ THÀNH CÔNG');
      }
      setTimeout(() => navigate('/product-variants'), 1000);
    } catch (error: any) {
      showToast('error', error.response?.data?.message || 'CÓ LỖI XẢY RA KHI LƯU');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title={isEditMode ? 'Chỉnh Sửa Biến Thể' : 'Tạo Mới Biến Thể'}
        subtitle="Cấu hình chi tiết mã hàng, thuộc tính và bảng giá đa quy cách"
        onBack={() => navigate('/product-variants')}
        icon={Package}
      />

      {/* 🔥 TABS ĐIỀU HƯỚNG 3 BƯỚC */}
      <div className="mb-5 flex items-center justify-between">
        <TabGroup>
          <TabButton
            active={activeTab === 'info'}
            onClick={() => setActiveTab('info')}
            label="1. THÔNG TIN CƠ BẢN"
            icon={Info}
          />
          <TabButton
            active={activeTab === 'attributes'}
            onClick={() => setActiveTab('attributes')}
            label="2. THUỘC TÍNH CHI TIẾT"
            icon={Layers}
          />
          <TabButton
            active={activeTab === 'pricing'}
            onClick={() => setActiveTab('pricing')}
            label={`3. QUY CÁCH BÁN HÀNG (${formData.prices.length})`}
            icon={Banknote}
          />
        </TabGroup>
      </div>

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-6">
          {/* ================= TAB 1: THÔNG TIN CƠ BẢN ================= */}
          <div
            className={
              activeTab === 'info'
                ? 'block animate-in fade-in slide-in-from-bottom-4 duration-300'
                : 'hidden'
            }
          >
            <div className="flex flex-col gap-10">
              <FormSection title="Định Danh Biến Thể">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div className="md:col-span-2 mb-2 p-4 bg-blue-50/50 border border-blue-100 rounded-xl">
                    <FormSelect
                      label="Sản Phẩm Gốc (Cha)"
                      required
                      placeholder="Chọn sản phẩm..."
                      showSearch
                      searchPlaceholder="Tìm kiếm sản phẩm (theo tên, mã)..."
                      options={productOptions}
                      value={formData.productId}
                      error={errors.productId}
                      onSelect={(val) => handleFieldChange('productId', val)}
                      disabled={isEditMode}
                    />
                    {!isEditMode && (
                      <p className="text-xs text-blue-600 mt-2 font-medium italic">
                        * Thuộc tính động sẽ tự tải dựa trên Sản phẩm gốc bạn chọn.
                      </p>
                    )}
                  </div>
                  <FormInput
                    label="Mã SKU (Barcode)"
                    required
                    placeholder="VD: SKU-DAUTAY-500G"
                    value={formData.code}
                    error={errors.code}
                    disabled={loading}
                    onChange={(e) => handleFieldChange('code', e.target.value)}
                  />
                  <FormInput
                    label="Tên hiển thị biến thể"
                    required
                    placeholder="VD: Dâu Tây Đà Lạt - Hộp 500g"
                    value={formData.name}
                    error={errors.name}
                    disabled={loading}
                    onChange={(e) => handleFieldChange('name', e.target.value)}
                  />
                  <FormSelect
                    label="Trạng thái kinh doanh"
                    required
                    value={formData.isActive ? 1 : 0}
                    options={STATUS_OPTIONS}
                    onSelect={(val) => handleFieldChange('isActive', val === 1)}
                  />
                  <div className="flex flex-col">
                    <FormInput
                      label="Mức tồn kho an toàn tối thiểu (Safety Stock)"
                      type="number"
                      placeholder="0"
                      value={formData.inventoryGuideline === 0 ? '' : formData.inventoryGuideline}
                      error={errors.inventoryGuideline}
                      disabled={loading}
                      onChange={(e) =>
                        handleFieldChange('inventoryGuideline', parseInt(e.target.value, 10) || 0)
                      }
                    />
                    <p className="text-[11px] text-slate-400 mt-1 font-medium italic">
                      * Hệ thống sẽ cảnh báo khi tồn kho thực tế thấp hơn mức này (Mặc định: 0 -
                      Không cảnh báo).
                    </p>
                  </div>
                </div>
              </FormSection>
              <FormSection title="Hình Ảnh & Bổ Sung">
                <div className="flex flex-col gap-6">
                  <FormInput
                    label="Đường dẫn Ảnh (URL)"
                    placeholder="https://..."
                    value={formData.imagePath || ''}
                    onChange={(e) => handleFieldChange('imagePath', e.target.value)}
                  />
                  {formData.imagePath && (
                    <div className="w-24 h-24 rounded-xl border border-slate-200 overflow-hidden shadow-sm">
                      <img
                        src={formData.imagePath}
                        alt="Preview"
                        className="w-full h-full object-cover"
                        onError={(e) => (e.currentTarget.style.display = 'none')}
                      />
                    </div>
                  )}
                  <FormTextarea
                    label="Mô tả thêm (Tùy chọn)"
                    placeholder="Nhập mô tả riêng cho biến thể này..."
                    value={formData.description || ''}
                    rows={3}
                    onChange={(e: any) => handleFieldChange('description', e.target.value)}
                  />
                </div>
              </FormSection>

              <FormSection title="Kích Thước Đóng Gói & Khối Lượng (Physical Dimensions & Weight)">
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-6">
                  <FormInput
                    label="Dài (cm)"
                    type="number"
                    placeholder="VD: 30"
                    value={formData.lengthCm ?? ''}
                    disabled={loading}
                    onChange={(e) =>
                      handleDimensionChange(
                        'lengthCm',
                        e.target.value ? parseFloat(e.target.value) : undefined
                      )
                    }
                  />
                  <FormInput
                    label="Rộng (cm)"
                    type="number"
                    placeholder="VD: 20"
                    value={formData.widthCm ?? ''}
                    disabled={loading}
                    onChange={(e) =>
                      handleDimensionChange(
                        'widthCm',
                        e.target.value ? parseFloat(e.target.value) : undefined
                      )
                    }
                  />
                  <FormInput
                    label="Cao (cm)"
                    type="number"
                    placeholder="VD: 15"
                    value={formData.heightCm ?? ''}
                    disabled={loading}
                    onChange={(e) =>
                      handleDimensionChange(
                        'heightCm',
                        e.target.value ? parseFloat(e.target.value) : undefined
                      )
                    }
                  />
                  <FormInput
                    label="Thể Tích (CBM - m³)"
                    type="number"
                    placeholder="Tự động tính"
                    value={formData.unitCbm ?? ''}
                    disabled={loading}
                    onChange={(e) =>
                      handleFieldChange(
                        'unitCbm',
                        e.target.value ? parseFloat(e.target.value) : undefined
                      )
                    }
                  />
                  <FormInput
                    label="Khối Lượng Cả Bì (Kg)"
                    type="number"
                    placeholder="VD: 1.5"
                    value={formData.grossWeightKg ?? ''}
                    disabled={loading}
                    onChange={(e) =>
                      handleFieldChange(
                        'grossWeightKg',
                        e.target.value ? parseFloat(e.target.value) : undefined
                      )
                    }
                  />
                </div>
              </FormSection>
            </div>
          </div>

          {/* ================= TAB 2: THUỘC TÍNH (EAV) ================= */}
          <div
            className={
              activeTab === 'attributes'
                ? 'block animate-in fade-in slide-in-from-bottom-4 duration-300'
                : 'hidden'
            }
          >
            <FormSection title="Thuộc Tính Bổ Sung">
              {!formData.productId ? (
                <div className="p-12 text-center bg-slate-50 border border-slate-200 border-dashed rounded-xl">
                  <Layers className="mx-auto text-slate-300 mb-3" size={40} />
                  <h4 className="text-base font-bold text-slate-600 mb-1">
                    Chưa chọn Sản Phẩm Gốc
                  </h4>
                  <p className="text-slate-400 font-medium text-sm">
                    Vui lòng quay lại Tab 1 và chọn Sản Phẩm Gốc.
                  </p>
                </div>
              ) : dynamicAttributes.length === 0 ? (
                <div className="p-8 text-center bg-slate-50 border border-slate-200 border-dashed rounded-xl">
                  <p className="text-slate-500 font-medium">
                    Sản phẩm này không yêu cầu cấu hình thuộc tính động.
                  </p>
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6 p-6 bg-indigo-50/30 border border-indigo-100 rounded-xl">
                  {dynamicAttributes.map((def) => (
                    <FormInput
                      key={def.id}
                      label={def.name}
                      required={def.isRequired}
                      placeholder={`Nhập ${def.name.toLowerCase()}...`}
                      value={getDynamicAttrValue(def.id)}
                      error={errors[`attr_${def.id}`]}
                      onChange={(e) => handleDynamicAttrChange(def.id, e.target.value)}
                    />
                  ))}
                </div>
              )}
            </FormSection>
          </div>

          {/* ================= TAB 3: BẢNG GIÁ ĐA QUY CÁCH ================= */}
          <div
            className={
              activeTab === 'pricing'
                ? 'block animate-in fade-in slide-in-from-bottom-4 duration-300'
                : 'hidden'
            }
          >
            <FormSection title="Thiết Lập Quy Cách Bán Hàng">
              <p className="text-sm text-slate-500 mb-4 -mt-2">
                Khai báo các đơn vị tính khách hàng có thể mua (VD: Bán theo Kg, bán theo Thùng).
                Dòng được tích <strong>Mặc định</strong> sẽ hiển thị trên mặt tiền của website.
              </p>

              {errors.prices && (
                <div className="mb-4 p-3.5 bg-rose-50 text-rose-600 text-sm font-bold rounded-xl border border-rose-200 flex items-start gap-2.5 shadow-2xs">
                  <AlertCircle size={18} className="shrink-0 mt-0.5 text-rose-500" />
                  <span className="leading-snug">{errors.prices}</span>
                </div>
              )}

              {/* TABLE NHẬP LIỆU BẢNG GIÁ - XỬ LÝ OVERFLOW DROPDOWN */}
              <div className="border border-slate-200 rounded-2xl bg-white shadow-2xs">
                <table className="w-full text-left border-collapse">
                  <thead className="bg-slate-50/80 border-b border-slate-200">
                    <tr>
                      <th className="w-[38%] py-3.5 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider">
                        Đơn Vị Tính <span className="text-red-500">*</span>
                      </th>
                      <th className="w-[38%] py-3.5 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider">
                        Giá Bán Niêm Yết (VNĐ) <span className="text-red-500">*</span>
                      </th>
                      <th className="w-[12%] py-3.5 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                        Mặc Định
                      </th>
                      <th className="w-[12%] py-3.5 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                        Thao Tác
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {formData.prices.length === 0 ? (
                      <tr>
                        <td colSpan={4} className="py-10 text-center text-slate-400 text-sm italic">
                          Chưa có bảng giá nào. Vui lòng bấm nút thêm quy cách bán bên dưới.
                        </td>
                      </tr>
                    ) : (
                      formData.prices.map((row, idx) => (
                        <tr
                          key={idx}
                          className={`relative transition-colors ${
                            row.isDefault ? 'bg-yellow-50/30' : 'hover:bg-slate-50/60'
                          }`}
                          style={{ zIndex: 60 - idx }}
                        >
                          <td className="p-3 align-top">
                            <FormSelect
                              label=""
                              showSearch
                              searchPlaceholder="Tìm ĐVT..."
                              options={uomOptions}
                              value={row.uoMId}
                              onSelect={(val) => handleUpdatePriceRow(idx, 'uoMId', val)}
                              placeholder="Chọn ĐVT..."
                            />
                            {/* Chú thích quy cách / Quy đổi ĐVT */}
                            {(() => {
                              if (!row.uoMId) return null;
                              if (currentProd && row.uoMId === currentProd.baseUoMId) {
                                return (
                                  <div className="mt-1.5 inline-flex items-center gap-1.5 text-[11px] font-bold text-amber-800 bg-amber-50/90 px-2.5 py-1 rounded-lg border border-amber-200 shadow-2xs">
                                    <Star size={11} className="fill-amber-500 text-amber-500" />
                                    <span>Đơn vị cơ sở ({currentProd.baseUoMName || 'Kg'})</span>
                                  </div>
                                );
                              }
                              const conv =
                                uomConversions.find(
                                  (c) =>
                                    c.isActive &&
                                    c.productId === formData.productId &&
                                    c.fromUoMId === row.uoMId
                                ) ||
                                uomConversions.find(
                                  (c) => c.isActive && !c.productId && c.fromUoMId === row.uoMId
                                );
                              if (conv) {
                                const fromName =
                                  conv.fromUoMName ||
                                  uomOptions.find((u) => u.value === row.uoMId)?.label?.split(' ')[0] ||
                                  'ĐVT';
                                const toName =
                                  conv.toUoMName ||
                                  currentProd?.baseUoMName ||
                                  'ĐV cơ sở';
                                return (
                                  <div className="mt-1.5 inline-flex items-center gap-1.5 text-[11px] font-bold text-emerald-800 bg-emerald-50 px-2.5 py-1 rounded-lg border border-emerald-200/90 shadow-2xs">
                                    <span className="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                                    <span>
                                      1 {fromName} = {conv.conversionFactor.toLocaleString('vi-VN')} {toName}
                                    </span>
                                  </div>
                                );
                              }
                              return (
                                <div className="mt-1.5 flex flex-wrap items-center gap-1.5">
                                  <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-rose-600 bg-rose-50 px-2 py-0.5 rounded-md border border-rose-200">
                                    ⚠️ Chưa cấu hình quy đổi
                                  </span>
                                  <button
                                    type="button"
                                    onClick={() => handleOpenQuickConversionModal(row.uoMId, idx)}
                                    className="inline-flex items-center gap-1 px-2.5 py-0.5 text-[11px] font-bold text-amber-800 bg-amber-100 hover:bg-amber-200 border border-amber-300 rounded-md transition-all cursor-pointer shadow-2xs hover:shadow-xs active:scale-98"
                                    title="Mở popup cấu hình tỷ lệ quy đổi đặc thù ngay"
                                  >
                                    <Plus size={11} strokeWidth={3} />
                                    Cấu hình ngay
                                  </button>
                                </div>
                              );
                            })()}
                          </td>
                          <td className="p-3 align-top">
                            <div className="relative">
                              <input
                                type="text"
                                inputMode="numeric"
                                placeholder="VD: 45.000"
                                value={row.price > 0 ? row.price.toLocaleString('vi-VN') : ''}
                                onChange={(e) => {
                                  const numericVal =
                                    parseInt(e.target.value.replace(/\D/g, ''), 10) || 0;
                                  handleUpdatePriceRow(idx, 'price', numericVal);
                                }}
                                className="w-full h-11.5 px-4 pr-10 rounded-xl border border-slate-200 focus:border-yellow-400 focus:ring-4 focus:ring-yellow-400/20 bg-slate-50/50 hover:bg-white focus:bg-white text-slate-800 font-bold text-sm outline-none transition-all"
                              />
                              <span className="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 text-xs font-bold pointer-events-none">
                                ₫
                              </span>
                            </div>
                          </td>
                          <td className="p-3 text-center align-middle">
                            <button
                              type="button"
                              onClick={() => handleSetDefaultPrice(idx)}
                              className={`p-2.5 rounded-full transition-all mx-auto ${
                                row.isDefault
                                  ? 'text-yellow-500 bg-yellow-100 shadow-xs'
                                  : 'text-slate-300 hover:bg-slate-100 hover:text-slate-500'
                              }`}
                              title={row.isDefault ? 'Đang làm mặc định' : 'Đặt làm mặc định'}
                            >
                              <Star size={20} className={row.isDefault ? 'fill-current' : ''} />
                            </button>
                          </td>
                          <td className="p-3 text-center align-middle">
                            <button
                              type="button"
                              onClick={() => handleRemovePriceRow(idx)}
                              className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors mx-auto cursor-pointer"
                              title="Xóa quy cách"
                            >
                              <Trash2 size={18} strokeWidth={2.5} />
                            </button>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>

                {/* NÚT THÊM DÒNG & CẤU HÌNH QUY ĐỔI DƯỚI ĐÁY BẢNG */}
                <div className="p-3.5 bg-slate-50/50 border-t border-slate-100 flex flex-wrap items-center justify-center gap-3">
                  <button
                    type="button"
                    onClick={handleAddPriceRow}
                    className="flex items-center gap-2 px-5 py-2.5 text-sm font-bold text-amber-700 bg-amber-50 hover:bg-amber-100 border border-amber-200 rounded-xl transition-all cursor-pointer shadow-2xs hover:shadow-xs active:scale-98"
                  >
                    <Plus size={16} strokeWidth={3} /> THÊM QUY CÁCH BÁN
                  </button>

                  <button
                    type="button"
                    onClick={() => handleOpenQuickConversionModal()}
                    className="flex items-center gap-2 px-4 py-2.5 text-sm font-bold text-slate-700 bg-white hover:bg-slate-50 border border-slate-200 rounded-xl transition-all cursor-pointer shadow-2xs hover:shadow-xs active:scale-98"
                    title="Mở popup thêm quy đổi đặc thù cho sản phẩm này"
                  >
                    <Repeat size={15} className="text-amber-500" />
                    Cấu hình quy đổi đặc thù
                  </button>
                </div>
              </div>
            </FormSection>
          </div>

          {/* ================= FOOTER BUTTONS ================= */}
          <div className="flex justify-between items-center pt-6 border-t border-slate-100 mt-2">
            {activeTab === 'info' ? (
              <div />
            ) : (
              <button
                type="button"
                onClick={() => setActiveTab(activeTab === 'pricing' ? 'attributes' : 'info')}
                className="px-6 py-2.5 rounded-xl font-bold text-sm text-slate-600 bg-slate-100 hover:bg-slate-200 transition-colors"
              >
                Lùi lại bước trước
              </button>
            )}

            {activeTab !== 'pricing' ? (
              <button
                type="button"
                onClick={() => setActiveTab(activeTab === 'info' ? 'attributes' : 'pricing')}
                className="flex items-center gap-2 px-6 py-2.5 rounded-xl font-bold text-sm text-slate-800 bg-yellow-400 hover:bg-yellow-500 transition-colors shadow-sm shadow-yellow-200"
              >
                Tiếp tục <ChevronRight size={18} />
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

      {/* MODAL POPUP CẤU HÌNH QUY ĐỔI ĐẶC THÙ NHANH CHO SẢN PHẨM */}
      <QuickUoMConversionModal
        isOpen={quickConvModal.isOpen}
        onClose={() => setQuickConvModal({ isOpen: false })}
        productId={formData.productId}
        productName={currentProd?.name}
        productCode={currentProd?.code}
        baseUoMId={currentProd?.baseUoMId}
        baseUoMName={currentProd?.baseUoMName}
        initialFromUoMId={quickConvModal.fromUoMId}
        uomOptions={uomOptions}
        onSuccess={handleQuickConversionSuccess}
      />
    </PageContainer>
  );
};

export default ProductVariantForm;
