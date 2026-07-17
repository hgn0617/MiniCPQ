(() => {
  const form = document.querySelector("#approval-pricing-form");
  if (!form) return;

  const cost = Number.parseFloat(form.dataset.cost);
  const marginInput = form.querySelector("#gross-margin-percent");
  const priceInput = form.querySelector("#final-price");
  const profitOutput = form.querySelector("#pricing-profit");
  const messageOutput = form.querySelector("#pricing-message");
  const approveButton = form.querySelector("#approve-quote-button");
  const currency = new Intl.NumberFormat("zh-CN", {
    style: "currency",
    currency: "CNY",
    minimumFractionDigits: 2
  });

  const roundMoney = value => Math.round((value + Number.EPSILON) * 100) / 100;

  function refreshSummary(price) {
    const isValid = Number.isFinite(price) && price > 0 && price >= cost;
    approveButton.disabled = !isValid;

    if (!Number.isFinite(price) || price <= 0) {
      profitOutput.textContent = "—";
      messageOutput.textContent = "请输入有效的最终售价";
      messageOutput.classList.add("pricing-warning");
      return;
    }

    profitOutput.textContent = currency.format(price - cost);
    messageOutput.textContent = price < cost ? "售价不能低于成本" : "售价和毛利率将在批准时冻结";
    messageOutput.classList.toggle("pricing-warning", price < cost);
  }

  function updatePriceFromMargin() {
    const margin = Number.parseFloat(marginInput.value);
    if (!Number.isFinite(margin) || margin < 0 || margin >= 100) {
      priceInput.value = "";
      refreshSummary(Number.NaN);
      messageOutput.textContent = "毛利率必须在 0%（含）到 100%（不含）之间";
      return;
    }

    const price = roundMoney(cost / (1 - margin / 100));
    priceInput.value = price.toFixed(2);
    refreshSummary(price);
  }

  function updateMarginFromPrice() {
    const price = Number.parseFloat(priceInput.value);
    if (!Number.isFinite(price) || price <= 0) {
      marginInput.value = "";
      refreshSummary(price);
      return;
    }

    const margin = (price - cost) / price * 100;
    marginInput.value = margin.toFixed(2);
    refreshSummary(price);
  }

  marginInput.addEventListener("input", updatePriceFromMargin);
  priceInput.addEventListener("input", updateMarginFromPrice);
  form.addEventListener("submit", event => {
    const price = Number.parseFloat(priceInput.value);
    if (!Number.isFinite(price) || price <= 0 || price < cost) {
      event.preventDefault();
      refreshSummary(price);
      priceInput.focus();
      return;
    }

    if (!window.confirm("确定以当前售价批准这张报价吗？")) {
      event.preventDefault();
    }
  });

  updatePriceFromMargin();
})();
