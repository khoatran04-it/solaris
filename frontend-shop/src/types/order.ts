export interface ShopOrderItem {
  detailId: number;
  variantId: number;
  variantName: string;
  variantCode: string;
  imagePath?: string;
  uoMId: number;
  uoMName: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  totalPrice: number;
  issuedQuantity: number;
}

export interface ShopOrder {
  id: number;
  orderCode: string;
  orderDate: string;
  status: number;
  statusName: string;
  paymentStatus: number;
  paymentStatusName: string;
  paymentMethod: number;
  paymentMethodName: string;
  subTotal: number;
  discountAmount: number;
  shippingFee: number;
  totalAmount: number;
  receiverName?: string;
  receiverPhone?: string;
  deliveryAddress?: string;
  trackingCode?: string;
  shippingProvider?: string;
  expectedDeliveryDate?: string;
  note?: string;
  cancellationReason?: string;
  items: ShopOrderItem[];
}

export interface ShopCheckoutPayload {
  customerAddressId?: number;
  receiverName?: string;
  receiverPhone?: string;
  province?: string;
  district?: string;
  ward?: string;
  streetAddress?: string;
  ghnDistrictId?: number;
  ghnWardCode?: string;
  shippingFee?: number;
  latitude?: number;
  longitude?: number;
  paymentMethod: number;
  note?: string;
}

export interface ShopOrderCancelPayload {
  reason: string;
}
