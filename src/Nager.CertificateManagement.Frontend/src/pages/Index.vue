<template>
  <q-page padding>
    <q-card class="AddCertificateJob q-mb-md">
      <q-card-section>
        <q-input
          v-model="fqdn"
          dense
          class="q-mb-sm"
          label="Fully Qualified Domain Name (e.g. test.nager.at)"
        />

        <q-btn
          color="primary"
          label="Add certificate job"
          @click="create"
        />
      </q-card-section>
    </q-card>

    <q-table
      title="Certificate Jobs"
      dense
      :data="certificateJobs"
      :columns="columns"
      row-key="id"
    />
  </q-page>
</template>

<script>
export default {
  name: 'PageIndex',
  data () {
    return {
      columns: [
        { name: 'FQDN', align: 'left', label: 'FQDN', field: 'fqdn' },
        { name: 'Created', align: 'left', label: 'Created', field: 'created' },
        { name: 'Updated', align: 'left', label: 'Updated', field: 'updated' },
        { name: 'Available', align: 'left', label: 'Available', field: 'isAvailable' }
      ],
      fqdn: '',
      certificateJobs: []
    }
  },
  async created () {
    await this.getAll()
  },
  methods: {
    async getAll () {
      try {
        var response = await this.$axios.get('/api/CertificateJob')
        this.certificateJobs = response.data
      } catch (error) {
        this.$q.notify({
          type: 'negative',
          message: 'Request failure',
          caption: `${error}`
        })
      }
    },
    async create () {
      try {
        await this.$axios.post('/api/CertificateJob', { fqdn: this.fqdn })
        await this.getAll()
      } catch (error) {
        this.$q.notify({
          type: 'negative',
          message: 'Request failure',
          caption: `${error}`
        })
      }
    }
  }
}
</script>
