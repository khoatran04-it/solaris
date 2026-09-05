export interface VnPayPaymentPayload {
  orderCode: string;
  orderDescription?: string;
  bankCode?: string;
}

export interface VnPayPaymentResponse {
  paymentUrl: string;
  orderCode: string;
}

export interface VnPayCallbackResult {
  isSuccess: boolean;
  orderCode?: string;
  transactionNo?: string;
  responseCode?: string;
  bankCode?: string;
  amount: number;
  orderInfo?: string;
  message?: string;
}
